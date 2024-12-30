
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
        using NaughtyAttributes;
        using Game.DB;

        [UnitySingleton(UnitySingletonAttribute.Type.ExistsInScene)]
        public class TimeManager : UnitySingleton<TimeManager>
        {
            [BoxGroup("필수"), Label("시간 표시 텍스트")]
            public Text timeText;

            [BoxGroup("설정"), Label("게임 시간, 초")]
            public float gameTime;

            // 타이머 패치
            // 게임 시작 이후 경과 시간 저장
            public int Hour { get; private set; } = 0;   // 시
            public int Min { get; private set; } = 0;    // 분
            public int Sec { get; private set; } = 0;    // 초

            private int presentedMin = 0;
            private int presentedSec = 0;
            private bool timePause;

            // 현재 시간
            public float CurrentTime {
                get { return currentTime; }
                set { currentTime = Mathf.Clamp(value, 0f, 3000f); } 
            } 
            private float currentTime;

            private void Start()
            {
                CurrentTime = gameTime;

                GameManager.Instance.gameOverEvent.AddListener(() => { timePause = true; });
            }

            // 변수에 시간 지점 체크
            public float Check(out float variable)
            {
                variable = CurrentTime;
                return CurrentTime;
            }
            public float Check()
            {
                return CurrentTime;
            }

            // [체크된 시간 변수]로부터 현재까지의 경과 시간이 targetTime 이상인지 검사
            public bool CheckTimeElapsed(ref float checkedTimeVariable, float targetTime)
            {
                return (checkedTimeVariable - CurrentTime >= targetTime);
            }

            void FixedUpdate()
            {
                //if (IsNotHost) return;
                //if (CurrentTime <= 0f)
                //{
                //    mGameManager.TimerEnd();
                //    return;
                //}

                // 시간 줄이기
                if (!timePause)
                {
                    CurrentTime -= Time.fixedDeltaTime;
                }

                
                Sec = (int)(CurrentTime % 60f);
                Min = (int)(CurrentTime / 60) % 60;
                Hour = (int)(CurrentTime / 3600);

                if (!PhotonNetwork.IsMasterClient) return;
                if (CurrentTime.Equals(0f) && !timePause)
                {
                    timePause = true;
                    GameManager.Instance.GameOver();
                    photonView.RPC("RPC_StopTimerSound", RpcTarget.All);
                }
            }

            public void TimerStart()
            {
                timePause = false;
                StartCoroutine(TimeElapseRoutine());
            }

            [PunRPC]
            private void RPC_StopTimerSound()
            {
                GameData.Instance.TimeClockSound.Stop();
            }

            /// <summary>
            /// <para/> 코루틴 : 시간 업데이트
            /// <para/> 
            /// </summary>
            private IEnumerator TimeElapseRoutine()
            {
                yield return new WaitForEndOfFrame();

                /* Thread */
                while (true)
                {
                    yield return new WaitForSeconds(0.1f);

                    if (!PhotonNetwork.IsMasterClient) continue;

                    photonView.RPC("RPC_HostToAll_UpdateTime", RpcTarget.All ,CurrentTime, Min, Sec);
                }
            }

            /// <summary>
            /// <para/> [RPC]
            /// <para/> 모든 로컬 클라이언트에 시간 텍스트 업데이트
            /// </summary>
            [PunRPC]
            private void RPC_HostToAll_UpdateTime(float _currentTime ,int mm, int ss)
            {
                CurrentTime = _currentTime;
                timeText.text = $"{mm.ToString("D2")}:{ss.ToString("D2")}";

                if (GameData.Instance.TimeClockSound.isPlaying) return;
                if (_currentTime < 10f) GameData.Instance.TimeClockSound.Play();
            }
        }
    }
}