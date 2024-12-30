using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Photon.Pun;

namespace Game
{
    namespace Server
    {
        [RequireComponent(typeof(PhotonView))]
        [DisallowMultipleComponent]
        public class NoLagPhotonPunTransformView : MonoBehaviourPunCallbacks, IPunObservable
        {
            // var
            [HideInInspector]
            public ViewType ViewType;

            // Network var
            //Values that will be synced over network
            Vector2 latestPos = Vector2.zero;
            Quaternion latestRot = Quaternion.identity;
            bool photonViewLock = false;    // Sync lock between Update() or FixedUpdate() and OnPhotonSerializeView(). 
                                            // It used for object that is able to transfer ownership.

            //Lag compensation
            float currentTime = 0f;
            double currentPacketTime = 0;
            double lastPacketTime = 0;
            Vector2 positionAtLastPacket = Vector2.zero;
            Quaternion rotationAtLastPacket = Quaternion.identity;

            // Unity Components
            private Rigidbody mRigidbody;
            private Rigidbody2D mRigidbody2D;

            // Start is called before the first frame update
            void Start()
            {
                Debug.Assert(photonView);
                //Debug.Log(ViewType);

                switch (ViewType)
                {
                    case ViewType.Rigidbody:
                        mRigidbody = GetComponent<Rigidbody>();
                        Debug.Assert(mRigidbody);
                        break;

                    case ViewType.Rigidbody2D:
                        mRigidbody2D = GetComponent<Rigidbody2D>();
                        Debug.Assert(mRigidbody2D);
                        break;
                }
            }

            // Update is called once per frame
            void FixedUpdate()
            {
                if (!photonView.IsMine)
                {
                    //if (photonViewLock) return;

                    //Debug.Log("Read Pos from " + photonView.OwnerActorNr);
                    //Debug.Log("Remote Player Name : " + gameObject.name);

                    //Lag compensation
                    double timeToReachGoal = currentPacketTime - lastPacketTime;
                    currentTime += Time.deltaTime;
                    transform.position = (Vector2.Lerp(positionAtLastPacket, latestPos, (float)(currentTime / timeToReachGoal)));

                    ////Update remote player
                    //switch (ViewType)
                    //{
                    //    case ViewType.Transform:
                    //        transform.position = (Vector2.Lerp(positionAtLastPacket, latestPos, (float)(currentTime / timeToReachGoal)));
                    //        //transform.rotation = (Quaternion.Lerp(rotationAtLastPacket, latestRot, (float)(currentTime / timeToReachGoal)));
                    //        break;

                    //    case ViewType.Rigidbody:
                    //        mRigidbody.MovePosition(Vector3.Lerp(positionAtLastPacket, latestPos, (float)(currentTime / timeToReachGoal)));
                    //        mRigidbody.MoveRotation(Quaternion.Lerp(rotationAtLastPacket, latestRot, (float)(currentTime / timeToReachGoal)));
                    //        break;

                    //    case ViewType.Rigidbody2D:
                    //        mRigidbody2D.MovePosition(Vector3.Lerp(positionAtLastPacket, latestPos, (float)(currentTime / timeToReachGoal)));
                    //        mRigidbody2D.MoveRotation(Quaternion.Lerp(rotationAtLastPacket, latestRot, (float)(currentTime / timeToReachGoal)));
                    //        break;

                    //    case ViewType.None:
                    //        break;
                    //}
                    return;
                }
            }

            public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
            {
                if (stream.IsWriting)
                {
                    stream.SendNext((Vector2)transform.position);
                    Debug.Log("Write Pos from " + photonView.OwnerActorNr);

                    //switch (ViewType)
                    //{
                    //    case ViewType.Transform:
                    //        stream.SendNext(transform.position);
                    //        Debug.Log("Write Pos from " + photonView.OwnerActorNr);
                    //        //stream.SendNext(transform.rotation);
                    //        break;

                    //    case ViewType.Rigidbody:
                    //        stream.SendNext(mRigidbody.position);
                    //        stream.SendNext(mRigidbody.rotation);
                    //        break;

                    //    case ViewType.Rigidbody2D:
                    //        stream.SendNext(mRigidbody2D.position);
                    //        stream.SendNext(mRigidbody2D.rotation);
                    //        break;

                    //    case ViewType.None:
                    //        break;
                    //}

                    //We own this player: send the others our data
                    //stream.SendNext(transform.position);
                    //stream.SendNext(transform.rotation);
                    //stream.SendNext(mRigidbody.position);
                    //stream.SendNext(mRigidbody.rotation);

                    //photonViewLock = true;
                }

                else
                {
                    Debug.Log("Read Pos from " + photonView.OwnerActorNr);
                    Debug.Log("Remote Player Name : " + gameObject.name);


                    //Network player, receive data
                    latestPos = (Vector2)stream.ReceiveNext();
                    //latestRot = (Quaternion)stream.ReceiveNext();

                    //Lag compensation
                    currentTime = 0.0f;
                    lastPacketTime = currentPacketTime;
                    currentPacketTime = info.SentServerTime;

                    positionAtLastPacket = transform.position;

                    //switch (ViewType)
                    //{
                    //    case ViewType.Transform:
                    //        Debug.Log("Read last packet update");
                    //        positionAtLastPacket = transform.position;
                    //        //rotationAtLastPacket = transform.rotation;
                    //        break;

                    //    case ViewType.Rigidbody:
                    //        positionAtLastPacket = mRigidbody.position;
                    //        rotationAtLastPacket = mRigidbody.rotation;
                    //        break;

                    //    case ViewType.Rigidbody2D:
                    //        positionAtLastPacket = mRigidbody2D.position;
                    //        break;

                    //    case ViewType.None:
                    //        break;
                    //}

                    //photonViewLock = false;
                }
            }

            public void ViewTypeUpdate(ViewType _viewType)
            {
                ViewType = _viewType;
                Debug.Log(ViewType + " Updated");
            }
        }

        public enum ViewType
        {
            None = -1,
            Transform,
            Rigidbody,
            Rigidbody2D
        }
    }
}