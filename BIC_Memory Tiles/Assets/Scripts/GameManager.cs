
namespace Game
{
    namespace Systems
    {
        using System.Collections;
        using System.Collections.Generic;
        using UnityEngine;
        using UnityEngine.UI;
        using UnityEngine.Events;
        using UnityEngine.SceneManagement;
        using Photon;
        using Photon.Pun;
        using Game.DB;
        using Game.Character;
        using Lib.Extension;

        [UnitySingleton(UnitySingletonAttribute.Type.ExistsInScene)]
        public class GameManager : UnitySingleton<GameManager>
        {
            // UI
            public Text redScoreText;
            public Text blueScoreText;
            public Text[] redWordMeanTextArr;
            public Text[] blueWordMeanTextArr;
            public Image[] winPlayerImgDataArr;
            public Image winPlayerImg;
            public Text winText;

            public int[] correctScore;  // 정답일 때 점수. 순서대로 3글자, 4글자, 5글자 득점 점수
            public int minusScore;      // 오답일 때 점수

            private int[] playerScores = new int[2];
            private bool[] playerLoadingComplete = new bool[2];

            // Events
            public UnityEvent gameOverEvent = new UnityEvent();

            // Start is called before the first frame update
            void Start()
            {
                GameData.Instance.LobbyBgm.Stop();

                redScoreText.Assert();
                blueScoreText.Assert();

                StartCoroutine(GameReadyRoutine());
            }

            public void ScoreUp(int _actorNum, int _wordNum)
            {
                photonView.RPC("RPC_ScoreUp", RpcTarget.All, _actorNum, _wordNum);
            }

            [PunRPC]
            private void RPC_ScoreUp(int _actorNum, int _wordNum)
            {
                playerScores[_actorNum] += correctScore[_wordNum];
                ScoreTextUpdate();
            }

            public void ScoreDown(int _actorNum)
            {
                photonView.RPC("RPC_ScoreDown", RpcTarget.All, _actorNum);
            }

            [PunRPC]
            private void RPC_ScoreDown(int _actorNum)
            {
                playerScores[_actorNum] -= 10;
                ScoreTextUpdate();
            }

            private void ScoreTextUpdate()
            {
                redScoreText.text = playerScores[(int)PlayerColor.RED].ToString();
                blueScoreText.text = playerScores[(int)PlayerColor.BLUE].ToString();
            }

            private IEnumerator GameReadyRoutine()
            {
                yield return null;

                // 게임씬 로드 완료를 상대에게도 전송
                photonView.RPC("RPC_CheckSceneLoad", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber - 1);

                // 모든 플레이어 대기
                var isGameReady = false;
                while (!isGameReady)
                {
                    isGameReady = true;
                    foreach (var check in playerLoadingComplete)
                    {
                        if (!check)
                            isGameReady = false;
                    }

                    yield return new WaitForSeconds(0.1f);
                }

                GameData.Instance.LoadGameData();

                for (int i = 0; i < 8; ++i)
                {
                    redWordMeanTextArr[i].text = GameData.Instance.redPlayerMeanDB[i];
                    blueWordMeanTextArr[i].text = GameData.Instance.bluePlayerMeanDB[i];
                }

                TimeManager.Instance.TimerStart();
                GameData.Instance.InGameBGM.Play();
            }

            [PunRPC]
            private void RPC_CheckSceneLoad(int _actorNum)
            {
                playerLoadingComplete[_actorNum] = true;
            }

            public void GameOver()
            {
                photonView.RPC("RPC_GameOver", RpcTarget.All);
            }

            [PunRPC]
            private IEnumerator RPC_GameOver()
            {
                yield return null;
                GameData.Instance.InGameBGM.Stop();
                GameData.Instance.WiningSound.Play();

                gameOverEvent.Invoke();
                
                // DRAW
                if (playerScores[0] == playerScores[1])
                {
                    winText.text = "DRAW";
                }

                else if (playerScores[0] > playerScores[1])
                {
                    winText.text = "WIN";
                    winPlayerImg.sprite = winPlayerImgDataArr[0].sprite;
                    winPlayerImg.enabled = true;
                }

                else
                {
                    winText.text = "WIN";
                    winPlayerImg.sprite = winPlayerImgDataArr[1].sprite;
                    winPlayerImg.enabled = true;
                }
                winText.enabled = true;

                yield return new WaitForSeconds(3f);

                if (PhotonNetwork.IsMasterClient)
                    PhotonNetwork.LoadLevel(0);
            }
        }
    }
}

