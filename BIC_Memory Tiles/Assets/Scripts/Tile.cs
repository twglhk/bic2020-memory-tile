
namespace Game
{
    namespace Map
    {
        using System.Collections;
        using System.Collections.Generic;
        using UnityEngine;
        using UnityEngine.UI;
        using Photon.Pun;
        using Game.Character;
        using Game.Systems;
        using Game.DB;
        using Lib.Extension;

        public class Tile : MonoBehaviourPunCallbacks
        {
            public SpriteRenderer spriteRenderer;
            public SpriteRenderer[] markerFrames;
            public Sprite tileImage;
            public Collider2D mCollider;
            public int alphabetNum;
            private int tileColorNum;
            private bool[] isMark = new bool[2]; 
         
            // Start is called before the first frame update
            void Start()
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                mCollider = GetComponent<BoxCollider2D>();

                spriteRenderer.Assert();
                mCollider.Assert();
                //GameData.Instance.AddTileInfo(this, tileNum);
            }

            public void SetInfo(int _alphabetNum, int _actorNum, int _posNum)
            {
                photonView.RPC("RPC_SetInfo", RpcTarget.All, _alphabetNum, _actorNum, _posNum);
            }

            [PunRPC]
            private void RPC_SetInfo(int _alphabetNum, int _actorNum, int _posNum)
            {
                // 알파벳 정보 세팅
                alphabetNum = _alphabetNum;

                // 이미지 캐싱
                tileImage = GameData.Instance.spriteContainer[_actorNum][_alphabetNum];
                tileColorNum = _actorNum;

                // 렌더러 이미지 세팅
                spriteRenderer.sprite = tileImage;
                spriteRenderer.sortingOrder = _posNum;
            }

            public void Marking(PlayerColor _actorColor)
            {
                var actorNum = (int)_actorColor;

                if (isMark[actorNum]) return;
  
                photonView.RPC("RPC_Marking", RpcTarget.All, actorNum);

                MarkingManager.Instance.UpdateMarkingUI(_actorColor, tileColorNum, alphabetNum, photonView.ViewID);
                //photonView.RPC("RPC_Marking", RpcTarget.All, _actorColor);
            }

            [PunRPC]
            private void RPC_Marking(int _actorColor)
            {
                GameData.Instance.MarkingSound.Play();

                // 이펙트 처리
                isMark[_actorColor] = true;

                if (isMark[0] && isMark[1])
                {
                    markerFrames[0].enabled = markerFrames[1].enabled = false;
                    markerFrames[2].enabled = true;
                }

                else
                    markerFrames[_actorColor].enabled = true;
            }

            [PunRPC]
            private void RPC_UnMarking(int _actorColor)
            {
                markerFrames[_actorColor].enabled = false;

                if (isMark[0] && isMark[1])
                {
                    markerFrames[2].enabled = false;

                    if (_actorColor == 0)
                        markerFrames[1].enabled = true;
                    else
                        markerFrames[0].enabled = true;
                }

                isMark[_actorColor] = false;
            }

            [PunRPC]
            public void RPC_DestroyTile()
            {
                foreach (var markerFrame in markerFrames)
                    markerFrame.enabled = false;
                spriteRenderer.enabled = false;
                mCollider.enabled = false;
            }
        }
    }
}