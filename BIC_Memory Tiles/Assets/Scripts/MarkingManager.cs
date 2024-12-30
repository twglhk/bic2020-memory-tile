
namespace Game
{
    namespace Systems
    {
        using System.Collections;
        using System.Collections.Generic;
        using UnityEngine;
        using UnityEngine.UI;
        using UnityEngine.Events;
        using Photon.Pun;
        using Photon;
        using Game.Character;
        using Game.Map;
        using Game.DB;
        using Lib.Extension;

        [UnitySingleton(UnitySingletonAttribute.Type.ExistsInScene)]
        public class MarkingManager : UnitySingleton<MarkingManager>
        {
            private bool isAllTileCleared = false;

            // Container
            private List<int> currentWordsList = new List<int>();        // 현재 단어 리스트
            private List<List<int>> targetList = new List<List<int>>();  // 정답 후보군을 저장하는 단어 리스트
            private List<List<int>> tileIDList = new List<List<int>>();  // 두 플레이어의 타일 ID 리스트를 저장하는 리스트
            private List<int> redTileIDList = new List<int>();           // 1P 플레이어의 타일 정보를 담는 리스트
            private List<int> blueTileIDList = new List<int>();          // 2P 플레이어의 타일 정보를 담는 리스트

            // UI
            private List<Image[]> imagesList = new List<Image[]>();     // 상단 마킹 단어 이미지 배열을 담은 리스트
            public Image[] playerRedMarkedWords;    // 1P 플레이어 상단 마킹 단어 이미지 배열. Inspector 설정
            public Image[] playerBlueMarkedWords;   // 2P 플레이어 상단 마킹 단어 이미지 배열. Inspector 설정
            private int[] cursors;                  // 두 플레이어의 상단 마킹 단어 UI 커서
            private const int MAX_CURSOR_POS = 6;   // 최대 단어 크기
            private List<Image[]> correctSignArrList = new List<Image[]>();
            public Image[] playerRedCorrectSign;    // 1P 플레이어의 상단 마킹 단어에 사용될 OX로 구성된 이미지 배열. Inspector 설정.
            public Image[] playerBlueCorrectSign;   // 2P 플레이어의 상단 마킹 단어에 사용될 OX로 구성된 이미지 배열. Inspector 설정.
            public Image[] correctSignImg;          // 단어 목록에 사용할 O 사인 이미지     
            private List<List<Image>> wordCorrectSignImgList = new List<List<Image>>();
            public List<Image> redWordCorrectSignImgList; // 1P 단어 목록에 사용될 O 사인 이미지 리스트
            public List<Image> blueWordCorrectSignImgList; // 2P 단어 목록에 사용될 O 사인 이미지 리스트

            // Events
            [HideInInspector]
            public UnityEvent LockMarking = new UnityEvent();

            [HideInInspector]
            public UnityEvent UnLockMarking = new UnityEvent();

            [HideInInspector]
            public System.Action<int> ActivateAttacking = (t) => { };

            // Start is called before the first frame update
            void Start()
            {
                // UI
                tileIDList.Add(redTileIDList);
                tileIDList.Add(blueTileIDList);
                imagesList.Add(playerRedMarkedWords);
                imagesList.Add(playerBlueMarkedWords);
                cursors = new int[2];
                cursors[0] = cursors[1] = 0;
                correctSignArrList.Add(playerRedCorrectSign);
                correctSignArrList.Add(playerBlueCorrectSign);
                wordCorrectSignImgList.Add(redWordCorrectSignImgList);
                wordCorrectSignImgList.Add(blueWordCorrectSignImgList);
            }

            public void UpdateMarkingUI(PlayerColor _actorColor, int _tileColorNum, int _alphabetNum, int _tileID)
            {
                // 글자 추가
                currentWordsList.Add(_alphabetNum);

                // 마킹 글자 및 UI 업데이트
                var actorNum = (int)_actorColor;
                photonView.RPC("RPC_UpdateTileID", RpcTarget.All, actorNum, _tileID);
                photonView.RPC("RPC_UpdateMarkingUI", RpcTarget.All, actorNum, _tileColorNum, _alphabetNum);

                // 정답/오답 체크
                var answerCheck = CheckWord(actorNum);
                Debug.Log(answerCheck);
                switch (answerCheck)
                {
                    case AnswerCheck.InCorrect:
                        photonView.RPC("RPC_PlayInCorrectSound", RpcTarget.All);  // 정답 소리 재생
                        GameManager.Instance.ScoreDown(actorNum);   // 패널티 처리

                        targetList.Clear();       // 타깃 단어 리스트 삭제
                        currentWordsList.Clear(); // 현재 단어 리스트 삭제

                        foreach(var id in tileIDList[actorNum])
                        {
                            PhotonNetwork.GetPhotonView(id).RPC("RPC_UnMarking", RpcTarget.All, actorNum);   // 타일 모두 언마킹
                        }

                        StartCoroutine(IncorrectLockingRoutine(actorNum));   // 오답 UI 처리 및 마킹 잠금
                        photonView.RPC("RPC_ClearTileIDList", RpcTarget.All, actorNum); // 오답 플레이어 타일 ID 리스트 초기화 요청
                        break;

                    case AnswerCheck.Possible:
                        break;

                    case AnswerCheck.Correct:
                        photonView.RPC("RPC_PlayCorrectSound", RpcTarget.All);  // 정답 소리 재생
                        GameManager.Instance.ScoreUp(actorNum, currentWordsList.Count - 3); // 점수 처리
                        
                        StartCoroutine(CorrectLockingRoutine(actorNum)); // 정답 UI 처리 및 마킹 잠금

                        // 게임 종료 조건 검사
                        if (isAllTileCleared)
                        {
                            GameManager.Instance.GameOver();
                            break;
                        }

                        targetList.Clear();        // 타깃 단어 리스트 삭제
                        currentWordsList.Clear();  // 내 현재 단어 리스트 삭제

                        //photonView.RPC("RPC_ClearMarkingUI", RpcTarget.All, actorNum); // 정답 플레이어 마킹 UI 모두 해제

                        // 상대 플레이어 타일 리스트에서 같은 것이 있는지 검사
                        int otherNum = 1;
                        if (actorNum.Equals(otherNum)) otherNum = 0;
                        foreach(var id in tileIDList[actorNum])
                        {
                            // 상대방 타일 리스트에서 정답 플레이어의 타일 ID와 동일한 것이 있는지 검색
                            int foundID = tileIDList[otherNum].Find((int tileID) => { 
                                return tileID == id; 
                            });
                            if (!foundID.Equals(default))
                            {
                                foreach (var othersViewID in tileIDList[otherNum])
                                {
                                    // 상대는 전부 오답처리를 위해 기록했던 타일 언마킹
                                    PhotonNetwork.GetPhotonView(othersViewID).RPC("RPC_UnMarking", RpcTarget.Others, otherNum);   
                                }

                                photonView.RPC("RPC_ClearTileIDList", RpcTarget.All, otherNum); // 상대 플레이어 타일 ID 리스트 초기화 요청
                                photonView.RPC("RPC_ClearMarkingUI", RpcTarget.All, otherNum);  // 상대 플레이어 마킹 UI 모두 삭제 요청
                                photonView.RPC("RPC_ClearWordList", RpcTarget.Others);          // 상대 플레이어의 타깃 단어 리스트와 현재 단어 리스트 삭제 요청
                            }

                            // 정답 타일 비활성화
                            var tilePhotonView = PhotonNetwork.GetPhotonView(id);
                            tilePhotonView.RPC("RPC_DestroyTile", RpcTarget.All);
                        }

                        photonView.RPC("RPC_ClearTileIDList", RpcTarget.All, actorNum);         // 정답 플레이어의 타일 ID 리스트 초기화 요청
                        break;
                }
            }

            [PunRPC]
            private void RPC_UpdateTileID(int _actorNum, int _tileID)
            {
                tileIDList[_actorNum].Add(_tileID);
            }

            [PunRPC]
            private void RPC_ClearTileIDList(int _actorNum)
            {
                tileIDList[_actorNum].Clear();
            }

            [PunRPC]
            private void RPC_UpdateMarkingUI(int _actorNum, int _tileColorNum, int _alphabetNum)
            {
                Debug.Log("RPC_UpdateMarkingUI");

                if (cursors[_actorNum].Equals(MAX_CURSOR_POS)) return;

                // 마킹 UI 업데이트
                imagesList[_actorNum][cursors[_actorNum]].sprite = GameData.Instance.spriteContainer[_tileColorNum][_alphabetNum];
                ChangeAlpha(imagesList[_actorNum][cursors[_actorNum]], 1f);
                cursors[_actorNum]++;
            }

            [PunRPC]
            private void RPC_ClearMarkingUI(int _actorNum)
            {
                foreach(var image in imagesList[_actorNum])
                {
                    ChangeAlpha(image, 0f);
                    image.sprite = null; 
                    cursors[_actorNum] = 0;
                }
            }

            /// <summary>
            /// 해당 플레이어의 현재 단어 모두 삭제
            /// </summary>
            [PunRPC]
            private void RPC_ClearWordList()
            {
                targetList.Clear();        // 타깃 단어 리스트 삭제
                currentWordsList.Clear();  // 내 현재 단어 리스트 삭제
            }

            [PunRPC]
            private void RPC_PlayCorrectSound()
            {
                GameData.Instance.CorrectSound.Play();
            }

            [PunRPC]
            private void RPC_PlayInCorrectSound()
            {
                GameData.Instance.InCorrectSound.Play();
            }

            private void ChangeAlpha(Image image, float alpha)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
            }

            private AnswerCheck CheckWord(int _playerColor)
            {
                AnswerCheck answerCheck = AnswerCheck.InCorrect;

                // 첫 글자 검색인 경우 => 1P, 2P를 포함한 전체 리스트에서 검색 후 타깃 단어 리스트에 추가
                if (currentWordsList.Count.Equals(1))
                {
                    // 1P 플레이어 단어 리스트 검색
                    for (int i = 0; i < GameData.Instance.redPlayerParsedWords.Count; ++i)
                    {
                        // 첫 글자가 해당 단어와 같다면 타깃 단어 리스트에 추가
                        if (currentWordsList[0] == GameData.Instance.redPlayerParsedWords[i][0])
                        {
                            targetList.Add(GameData.Instance.redPlayerParsedWords[i]);
                            answerCheck = AnswerCheck.Possible;
                            continue;
                        }
                    }

                    // 2P 플레이어 단어 리스트 검색
                    for (int i = 0; i < GameData.Instance.bluePlayerParsedWords.Count; ++i)
                    {
                        // 첫 글자가 해당 단어와 같다면 타깃 단어 리스트에 추가
                        if (currentWordsList[0] == GameData.Instance.bluePlayerParsedWords[i][0])
                        {
                            targetList.Add(GameData.Instance.bluePlayerParsedWords[i]);
                            answerCheck = AnswerCheck.Possible;
                            continue;
                        }
                    }
                }

                // 아닌 경우 => 타깃 단어 리스트에서만 검색
                else
                {
                    // 현재 비교할 단어 위치
                    int wordPos = currentWordsList.Count - 1;

                    // 타깃 단어 리스트에 있는 단어들을 검색
                    for (int i = 0; i < targetList.Count; ++i)
                    {
                        // 현재 마킹한 n번째 단어와 타깃 단어 리스트의 n번째 단어가 같은지 비교
                        if(currentWordsList[wordPos] == targetList[i][wordPos])
                        {
                            // 글자 수가 같은지 비교
                            if (currentWordsList.Count == targetList[i].Count)
                            {
                                // 해당 단어 DB에 존재하는지 확인하기 (중복 단어 방지)
                                int foundWordIndexFromRedDB, foundWordIndexFromBlueDB;
                                foundWordIndexFromRedDB = GameData.Instance.redPlayerParsedWords.FindIndex( target => {
                                    return target == targetList[i];
                                });
                                foundWordIndexFromBlueDB = GameData.Instance.bluePlayerParsedWords.FindIndex( target => { 
                                    return target == targetList[i]; 
                                });

                                // DB 검색 실패했다면 오답
                                if (foundWordIndexFromRedDB.Equals(-1) && foundWordIndexFromBlueDB.Equals(-1))
                                    answerCheck = AnswerCheck.InCorrect;

                                else
                                {
                                    // 양 플레이어에게 DB에서 해당 단어 제거 요청
                                    photonView.RPC("RPC_DeleteWord", RpcTarget.All, foundWordIndexFromRedDB, foundWordIndexFromBlueDB, _playerColor);

                                    // 2P가 1P의 단어를 맞췄다면 공격 활성화
                                    if (_playerColor.Equals(1) && !foundWordIndexFromRedDB.Equals(-1))
                                        ActivateAttacking.Invoke(1);

                                    // 1P가 2P의 단어를 맞췄다면 공격 활성화
                                    if (_playerColor.Equals(0) && !foundWordIndexFromBlueDB.Equals(-1))
                                        ActivateAttacking.Invoke(0);

                                    // 정답 처리
                                    answerCheck = AnswerCheck.Correct;

                                    // 게임 종료 조건 검사
                                    if (GameData.Instance.redPlayerParsedWords.Count.Equals(0)
                                        && GameData.Instance.bluePlayerParsedWords.Count.Equals(0))
                                        isAllTileCleared = true;
                                }

                                //Debug.Log(foundWordIndexFromRedDB);
                                //Debug.Log(foundWordIndexFromBlueDB);

                                //if (foundWordIndexFromRedDB != -1)
                                //    GameData.Instance.redPlayerParsedWords.RemoveAt(foundWordIndexFromRedDB);
                                //else if (foundWordIndexFromBlueDB != -1)
                                //    GameData.Instance.bluePlayerParsedWords.RemoveAt(foundWordIndexFromBlueDB);

                                break;
                            }
                            answerCheck = AnswerCheck.Possible;
                        }

                        // 같지 않다면
                        else
                        {
                            // 타깃 단어 리스트에서 해당 단어 제거 후 인덱스 하나 감소
                            targetList.RemoveAt(i--);
                        }
                    }
                }
                return answerCheck;
            }

            [PunRPC]
            private void RPC_DeleteWord(int _redDBIndex, int _blueDBIndex, int _playerColor)
            {
                Debug.Log(_redDBIndex);
                Debug.Log(_blueDBIndex);

                if (_redDBIndex != -1)
                {
                    wordCorrectSignImgList[0][_redDBIndex].sprite = correctSignImg[_playerColor].sprite;
                    ChangeAlpha(wordCorrectSignImgList[0][_redDBIndex], 1f);
                    wordCorrectSignImgList[0].RemoveAt(_redDBIndex);
                    GameData.Instance.redPlayerParsedWords.RemoveAt(_redDBIndex);
                }
                    
                else if (_blueDBIndex != -1)
                {
                    wordCorrectSignImgList[1][_blueDBIndex].sprite = correctSignImg[_playerColor].sprite;
                    ChangeAlpha(wordCorrectSignImgList[1][_blueDBIndex], 1f);
                    wordCorrectSignImgList[1].RemoveAt(_blueDBIndex);
                    GameData.Instance.bluePlayerParsedWords.RemoveAt(_blueDBIndex);
                }
                    
            }

            private IEnumerator IncorrectLockingRoutine(int _actorNum)
            {
                yield return null;

                LockMarking.Invoke();   // 마킹 잠금 이벤트 호출
                photonView.RPC("RPC_PopUpCorrectSign", RpcTarget.All, _actorNum, 1);    // 마킹 단어에 X 표시 요청

                yield return new WaitForSeconds(2f);

                UnLockMarking.Invoke(); // 마킹 잠금 해제 이벤트 호출
                photonView.RPC("RPC_HideCorrectSign", RpcTarget.All, _actorNum, 1);     // 마킹 단어에 X표시 제거 요청
                photonView.RPC("RPC_ClearMarkingUI", RpcTarget.All, _actorNum);         // 오답 플레이어 마킹 UI 모두 해제 요청
            }

            private IEnumerator CorrectLockingRoutine(int _actorNum)
            {
                yield return null;

                LockMarking.Invoke();   // 마킹 잠금 이벤트 호출
                photonView.RPC("RPC_PopUpCorrectSign", RpcTarget.All, _actorNum, 0);    // 마킹 단어에 O 표시 요청

                yield return new WaitForSeconds(2f);

                UnLockMarking.Invoke(); // 마킹 잠금 해제 이벤트 호출
                photonView.RPC("RPC_HideCorrectSign", RpcTarget.All, _actorNum, 0);     // 마킹 단어에 O표시 제거 요청
                photonView.RPC("RPC_ClearMarkingUI", RpcTarget.All, _actorNum);         // 정답 플레이어 마킹 UI 모두 해제
            }

            [PunRPC]
            private void RPC_PopUpCorrectSign(int _actorNum, int _correct)
            {
                ChangeAlpha(correctSignArrList[_actorNum][_correct], 1f);
            }

            [PunRPC]
            private void RPC_HideCorrectSign(int _actorNum, int _correct)
            {
                ChangeAlpha(correctSignArrList[_actorNum][_correct], 0f);
            }
        }

        public enum AnswerCheck
        {
            InCorrect,  // 오답
            Possible,   // 정답이 존재
            Correct     // 정답
        }
    }
}