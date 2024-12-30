
namespace Game
{ 
    namespace Character
    {
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using System;
        using Photon;
        using Photon.Pun;
        using UnityEngine;
        using UnityEngine.UI;
        using Lib.Extension;
        using Lib.Utility;
        using Game.Map;
        using Game.Systems;
        using Game.DB;

        public class CharacterHandler : MonoBehaviourPunCallbacks
        {
            public SpriteRenderer attackButtonImg;
            public Animator characterAnim;
            public GameObject DotCircle;
            public float moveSpeed;
            public float rotationSpeed;
            public float markRadius;
            public float attackRadius;
            public PlayerColor playerColor;
            private Vector2 moveDirection;
            private bool markingBan = false;
            private bool isOutofControl = false;
            private bool isAttackActive = false;

            // Start is called before the first frame update
            void Start()
            {
                characterAnim = GetComponent<Animator>();
                characterAnim.Assert();
                attackButtonImg.Assert();

                MarkingManager.Instance.LockMarking.AddListener(() => { markingBan = true; });
                MarkingManager.Instance.UnLockMarking.AddListener(() => { markingBan = false; });
                MarkingManager.Instance.ActivateAttacking += ((int _actorNum) => {
                    if (!_actorNum.Equals((int)playerColor)) return;

                    Debug.Log("Player Color Num : " + _actorNum+1);
                    Debug.Log("Actor Num : " + PhotonNetwork.LocalPlayer.ActorNumber);

                    isAttackActive = true;
                    photonView.RPC("RPC_AttackReady", RpcTarget.All);
                });
                GameManager.Instance.gameOverEvent.AddListener(() => { isOutofControl = true; });
            }

            // Update is called once per frame
            void Update()
            {
                if (!photonView.IsMine) return;
                if (isOutofControl) return;

                moveDirection = Vector2.zero;
                //moveDirection.x = Input.GetAxis("Horizontal");
                //moveDirection.y = Input.GetAxis("Vertical");

                /* 이동 페이즈 */
                if (Input.GetKey(KeyCode.RightArrow))
                    moveDirection.x = moveSpeed * Time.deltaTime;
                else if (Input.GetKey(KeyCode.LeftArrow))
                    moveDirection.x = -moveSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.UpArrow))
                    moveDirection.y = moveSpeed * Time.deltaTime;
                else if (Input.GetKey(KeyCode.DownArrow))
                    moveDirection.y = -moveSpeed * Time.deltaTime;

                Run();

                /* 마킹 페이즈 */
                if (Input.GetKeyDown(KeyCode.Space))
                    TileCheck();

                /* 공격 페이즈 */
                if (!isAttackActive) return;
                if (Input.GetKeyDown(KeyCode.A))
                    SearchTarget();
            }

            #region _Moving_
            private void Run()
            {
                characterAnim.SetFloat("Speed", moveDirection.x);

                if (moveDirection.Equals(Vector2.zero))
                {
                    characterAnim.SetInteger("AnimNum", 1);
                    return;
                }

                characterAnim.SetInteger("AnimNum", 0);

                transform.Translate(new Vector3(moveDirection.x, moveDirection.y));
            }
            #endregion // ==========================================================

            #region _Action_
            private void TileCheck()
            {
                var markedTile = CheckTileCollider();
                if (markedTile == null) return;
                if (markingBan) return;

                characterAnim.SetInteger("AnimNum", 3);
                markedTile.GetComponent<Tile>().Marking(playerColor);
            }

            public Collider2D CheckTileCollider()
            {
                var colliders = Physics2D.OverlapCircleAll(DotCircle.transform.position, markRadius);

                // 타일 콜라이더 찾기
                var targetCol = from col in colliders
                                where Layers.Compare(col.gameObject, Layers.Tile)  // 타일만 체크
                                select col;

                // 없으면 null 리턴
                var length = targetCol.Count();
                if (length == 0)
                    return null;

                // 있으면 거리 검사
                var targetArr = targetCol.ToArray();
                (float minDistance, int targetIndex)
                    = (Vector2.Distance(targetArr[0].transform.position, transform.position), 0);

                // 범위내 가장 가까운 대상 리턴
                float nowDistance;
                for (int i = 1; i < length; i++)
                {
                    nowDistance = Vector3.Distance(targetArr[i].transform.position, transform.position);

                    if (nowDistance < minDistance)
                        targetIndex = i;
                }

                return targetArr[targetIndex];
            }

            private void SearchTarget()
            {
                var colliders = Physics2D.OverlapCircleAll(transform.position, attackRadius);

                // 상대 캐릭터 콜라이더 찾기
                var targetCol = from col in colliders
                                where Layers.Compare(col.gameObject, Layers.Player) && col.transform != transform
                                select col;

                // 없으면 null 리턴
                var length = targetCol.Count();
                if (length == 0) return;

                // 공격 처리
                // 공격 애니메이션 추가
                Debug.Log("공격!");
                isAttackActive = false;
                characterAnim.SetInteger("AnimNum", 4);
                photonView.RPC("RPC_Attack", RpcTarget.All); // 이 플레이어 UI 이미지 비활성화 요청
                targetCol.ToArray()[0].gameObject.GetPhotonView().RPC("RPC_Hit", RpcTarget.Others); // 상대 플레이어 피격 처리
                //characterAnim.SetInteger("AnimNum", 1);
            }

            [PunRPC]
            private void RPC_AttackReady()
            {
                // 리모트 플레이어 어택 UI 활성화 요청
                attackButtonImg.enabled = true;
            }

            [PunRPC]
            private void RPC_Attack()
            {
                attackButtonImg.enabled = false;
                GameData.Instance.AttackSound.Play();
            }

            [PunRPC]
            private void RPC_Hit()
            {
                // 피격 효과 적용
                Invoke("Recovery", 3f);
                isOutofControl = true;

                // 피격 애니메이션 추가
                characterAnim.SetInteger("AnimNum", 5);
            }

            private void Recovery()
            {
                isOutofControl = false;
            }
            #endregion
        }

        public enum PlayerColor
        {
            NULL = -1,
            RED, BLUE
        }
    }
}