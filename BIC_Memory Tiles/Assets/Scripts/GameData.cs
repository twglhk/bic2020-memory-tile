
namespace Game
{
    namespace DB
    {
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using UnityEngine;
        using Photon;
        using Photon.Pun;
        using Game.Map;
        using Game.Character;

        [UnitySingleton(UnitySingletonAttribute.Type.ExistsInScene)]
        public class GameData : UnitySingleton<GameData>
        {
            public List<List<Sprite>> spriteContainer;
            public List<Sprite> yellowAlphabetSprites;
            public List<Sprite> brownAlphabetSprites;
            public List<string> redPlayerWordDB;
            public List<string> redPlayerMeanDB;
            public List<string> bluePlayerWordDB;
            public List<string> bluePlayerMeanDB;
            public List<List<int>> redPlayerParsedWords;
            public List<List<int>> bluePlayerParsedWords;
            public List<string> characterPrefabName;
            public Vector2[] spawnPos; // 임시 캐릭터 스폰 위치

            [HideInInspector]
            public List<Transform> spawnTransform;

            // Sounds
            public AudioSource LobbyBgm;
            public AudioSource AttackSound;
            public AudioSource WiningSound;
            public AudioSource TimeClockSound;
            public AudioSource InGameBGM;
            public AudioSource CorrectSound;
            public AudioSource InCorrectSound;
            public AudioSource MarkingSound;

            private void Awake()
            {
                DontDestroyOnLoad(gameObject);
                base.Awake();

                spriteContainer = new List<List<Sprite>>();
                redPlayerParsedWords = new List<List<int>>();
                bluePlayerParsedWords = new List<List<int>>();

                spriteContainer.Add(yellowAlphabetSprites);
                spriteContainer.Add(brownAlphabetSprites);
            }

            // Start is called before the first frame update
            void Start()
            {

            }

            public void LoadGameData()
            {
                StartCoroutine(CreateCharacters());
                StartCoroutine(CreateWordBlocksRoutine());
            }

            private IEnumerator CreateCharacters()
            {
                yield return null;

                Debug.Log("Actor Num : " + PhotonNetwork.LocalPlayer.ActorNumber);

                PhotonNetwork.Instantiate(characterPrefabName[PhotonNetwork.LocalPlayer.ActorNumber - 1],
                    spawnPos[PhotonNetwork.LocalPlayer.ActorNumber - 1], default);
            }

            private IEnumerator CreateWordBlocksRoutine()
            {
                yield return null;
                List<GameObject> spawnPos = GameObject.FindGameObjectsWithTag("Spawn").ToList<GameObject>();
                List<GameObject> spawnPosAlter = GameObject.FindGameObjectsWithTag("Spawn Alter").ToList<GameObject>();
                List<int> isUsedEven = new List<int>() {
                    0, 2, 4, 6,
                    9, 11, 13, 15,
                    16, 18, 20, 22,
                    25, 27, 29, 31,
                    32, 34, 36, 38,
                    41, 43, 45, 47,
                    48, 50, 52, 54,
                    57, 59, 61, 63
                };
                List<int> isUsedOdd = new List<int>()
                {
                    1,3,5,7,
                    8,10,12,14,
                    17,19,21,23,
                    24,26,28,30,
                    33,35,37,39,
                    40,42,44,46,
                    49,51,53,55,
                    56,58,60,62
                };

                // 1P 단어 파싱 및 블럭 생성 및 배치
                foreach (var word in redPlayerWordDB)
                {
                    var alphabets = word.ToCharArray();
                    var alphabetNumberList = new List<int>();

                    foreach (var alphabet in alphabets)
                    {
                        var convertedNum = ConvertAlphabetToNumber(alphabet);
                        alphabetNumberList.Add(convertedNum);   // 변환한 알파벳 번호 리스트에 추가

                        if (!PhotonNetwork.IsMasterClient) continue;

                        // 마스터 클라이언트 전용 타일 생성 및 정보 업데이트 전파
                        var randNum = UnityEngine.Random.Range(0, spawnPos.Count - 1);
                        var tileObj = PhotonNetwork.Instantiate("Prefabs/Tile", spawnPos[randNum].transform.position, default);
                        var tile = tileObj.GetComponentInChildren<Tile>();

                        var posNum = isUsedOdd[randNum];   // Sorting Order fix
                        //tileObj.transform.Translate(spawnPos[isUsedEven[randNum]].transform.position);
                        spawnPos.RemoveAt(randNum);
                        isUsedOdd.RemoveAt(randNum);

                        // 타일 정보 세팅
                        tile.SetInfo(convertedNum, (int)PlayerColor.RED, posNum);
                    }

                    // 변환된 단어를 플레이어의 변환 단어 리스트에 추가
                    redPlayerParsedWords.Add(alphabetNumberList);
                }

                // 2P 단어 파싱 및 블럭 생성
                foreach (var word in bluePlayerWordDB)
                {
                    var alphabets = word.ToCharArray();
                    var alphabetNumberList = new List<int>();

                    foreach (var alphabet in alphabets)
                    {
                        var convertedNum = ConvertAlphabetToNumber(alphabet);
                        alphabetNumberList.Add(convertedNum);   // 변환한 알파벳 번호 리스트에 추가

                        if (!PhotonNetwork.IsMasterClient) continue;

                        // 마스터 클라이언트 전용 타일 생성 및 정보 업데이트 전파
                        var randNum = UnityEngine.Random.Range(0, spawnPosAlter.Count - 1);
                        var tileObj = PhotonNetwork.Instantiate("Prefabs/Tile", spawnPosAlter[randNum].transform.position, default);
                        var tile = tileObj.GetComponentInChildren<Tile>();

                        var posNum = isUsedEven[randNum];
                        //tileObj.transform.Translate(spawnPos[isUsedOdd[randNum]].transform.position);
                        spawnPosAlter.RemoveAt(randNum);
                        isUsedEven.RemoveAt(randNum);

                        // 타일 정보 세팅
                        tile.SetInfo(convertedNum, (int)PlayerColor.BLUE, posNum);
                    }

                    // 변환된 단어를 플레이어의 변환 단어 리스트에 추가
                    bluePlayerParsedWords.Add(alphabetNumberList);
                }
            }

            public int ConvertAlphabetToNumber(char _alphabet)
            {
                return _alphabet - 97;
            }
        }
    }
}