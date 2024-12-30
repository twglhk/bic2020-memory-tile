
namespace Game
{
    namespace Server
    {
        using System.Collections;
        using System.Collections.Generic;
        using UnityEngine;
        using Photon;
        using Photon.Pun;
        using Photon.Realtime;
        using Game.Systems;

        [UnitySingleton(UnitySingletonAttribute.Type.ExistsInScene)]
        public class PhotonManager : UnitySingleton<PhotonManager>
        {
            private readonly string gameVersion = "2020.0.1";
            public int PlayerNumber = 1;

            private void Awake()
            {
                //DontDestroyOnLoad(gameObject);
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.AutomaticallySyncScene = true;
                PhotonNetwork.LogLevel = PunLogLevel.Informational;
            }

            // Start is called before the first frame update
            void Start()
            {
                Debug.Log("포톤 매니저 스타트"); 

                /* 네트워크 처리 */
                // 접속이 안 되어 있을 시 or 최초 접속 시
                if (!PhotonNetwork.IsConnected)
                {
                    Debug.Log("서버 접속 시도");
                    PhotonNetwork.ConnectUsingSettings();
                    return;
                }

                // 방에 참여 중이었다면
                if (PhotonNetwork.InRoom)
                {
                    Debug.Log("방 떠남!");
                    PhotonNetwork.LeaveRoom();
                }

                // 접속 중이었는데 방에 없었다면
                else
                {
                    PhotonNetwork.JoinLobby();
                }
            }

            #region _Server_
            public override void OnConnectedToMaster()
            {
                Debug.Log("서버에 연결됨");
                PhotonNetwork.JoinLobby();
            }

            public override void OnJoinedLobby()
            {
                Debug.Log("로비에 연결됨");
                LobbyManager.Instance.FadeOutTitle();
                //PhotonNetwork.LeaveLobby();
            }

            public void LeaveLobby()
            {
                PhotonNetwork.LeaveLobby();
            }

            public override void OnLeftLobby()
            {
                Debug.Log("로비 떠남");

                /* 방 생성 */
                PhotonNetwork.JoinRandomRoom();
            }

            public override void OnJoinedRoom()
            {
                Debug.Log("방 참가");

                /* 상대 기다리기 */
                StartCoroutine("GameReadyRoutine");
            }

            public override void OnJoinRandomFailed(short returnCode, string message)
            {
                Debug.Log("방 생성. 호스트가 됨");
                PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = (byte)PlayerNumber });
            }

            public override void OnPlayerEnteredRoom(Player other)
            {
                Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting


                if (PhotonNetwork.IsMasterClient)
                {
                    Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom
                }
            }

            public override void OnPlayerLeftRoom(Player other)
            {
                Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects

                if (PhotonNetwork.IsMasterClient)
                {
                    Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom
                }
            }

            private IEnumerator GameReadyRoutine()
            {
                yield return null;

                Debug.Log("상대를 기다리는 중");

                while (true)
                {
                    //Debug.Log("the number of players in the room : " + PhotonNetwork.CurrentRoom.PlayerCount);
                    //mCurrentPlayerNumberText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;

                    if (PhotonNetwork.CurrentRoom.PlayerCount.Equals(PhotonNetwork.CurrentRoom.MaxPlayers))
                    {
                        if (PhotonNetwork.IsMasterClient)
                            PhotonNetwork.LoadLevel(1);

                        yield break;
                    }
                    yield return new WaitForSeconds(1f);
                }
            }

            public override void OnDisconnected(DisconnectCause cause)
            {
                // 재접속 시도
                PhotonNetwork.ConnectUsingSettings();
            }

            public override void OnLeftRoom()
            {
                // 다시 시작 하면 로비 재참가
                PhotonNetwork.JoinLobby();
            }
            #endregion
        }
    }
}

