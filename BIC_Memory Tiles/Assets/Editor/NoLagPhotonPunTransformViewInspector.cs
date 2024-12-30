namespace REMBO
{
    namespace GameLib
    {
        namespace EditorScripts
        {
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;
            using UnityEngine;
            using UnityEditor;
            using Photon.Pun;
            using Game.Server;

            [CustomEditor(typeof(NoLagPhotonPunTransformView))]
            [CanEditMultipleObjects]
            public class NoLagPhotonPunTransformViewInspector : Editor
            {
                private NoLagPhotonPunTransformView noLagView = null;
                private ViewType viewTypeChoice;
                private static List<ViewType> mViewTypes = new List<ViewType>()
                { ViewType.Transform, ViewType.Rigidbody, ViewType.Rigidbody2D };

                void OnEnable()
                {
                    if (AssetDatabase.Contains(target))
                    {
                        noLagView = null;
                    }
                    else
                    {
                        noLagView = (NoLagPhotonPunTransformView)target;
                        viewTypeChoice = noLagView.ViewType;
                    }
                }

                public override void OnInspectorGUI()
                {
                    var update = true;

                    base.OnInspectorGUI();

                    // Prevents change from editor until playing.
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("Editing is disabled in play mode.", MessageType.Info);
                        EditorGUILayout.Space();
                        EditorGUI.BeginDisabledGroup(true);
                    }

                    // Add NoLagPhotonPunTransformView to GameObject PhotonView. 
                    if (!noLagView.photonView.ObservedComponents.Contains(noLagView))
                    {
                        noLagView.photonView.ObservedComponents.Add(noLagView);
                    }

                    // Pop-up Enums.
                    viewTypeChoice = (ViewType)EditorGUILayout.EnumPopup("Photon View Type", viewTypeChoice);

                    GUI.color = Color.green;
                    switch (viewTypeChoice)
                    {
                        case ViewType.Transform:
                            EditorGUILayout.HelpBox("If you use 'Transform' to move the object position, " +
                                "Use this option. Recommends when this object doesn't have 'Rigidbody' components." +
                                "It doesn't need PhotonTransformView", MessageType.Info);
                            // It doesn't add any photonViewComponents.
                            break;

                        case ViewType.Rigidbody:
                            if (!noLagView.GetComponent<Rigidbody2D>())
                            {
                                // Add 'PhotonRigidbodyView' to gameObject 'PhotonView' components
                                AddPhotonViewComponents<PhotonRigidbodyView>();
                                EditorGUILayout.HelpBox("If you use 'Rigidbody(3D)' to move the object position, " +
                                    "Use this option. For example, using Rigidbody.movePosition() or adjusting Rigidbody.position directly. " +
                                    "It has better performance than adjusting transform.position directly or using transform.Translate().", 
                                    MessageType.Info);
                            }

                            else
                            {
                                GUI.color = Color.red;
                                EditorGUILayout.HelpBox("You should remove Rigidbody2D from GameObject.", MessageType.Error);
                                update = false;
                                break;
                            }
                            break;

                        case ViewType.Rigidbody2D:
                            if (!noLagView.GetComponent<Rigidbody>())
                            {
                                // Add 'PhotonRigidbody2DView' to gameObject 'PhotonView' components
                                AddPhotonViewComponents<PhotonRigidbody2DView>();
                                EditorGUILayout.HelpBox("If you use 'Rigidbody(2D)' to move the object position, " +
                                    "Use this option. For example, using Rigidbody.movePosition() or adjusting Rigidbody.position directly. " +
                                    "It has better performance than adjusting transform.position directly or using transform.Translate().", 
                                    MessageType.Info);
                            }
                                
                            else
                            {
                                GUI.color = Color.red;
                                EditorGUILayout.HelpBox("You should remove Rigidbody(3D) from GameObject.", MessageType.Error);
                                update = false;
                                break;
                            }
                            break;

                        case ViewType.None:
                            GUI.color = Color.red;
                            EditorGUILayout.HelpBox("No view type set.", MessageType.Warning);
                            break;
                    }

                    if (GUI.changed && (int)viewTypeChoice >= 0 && update)
                        noLagView.ViewTypeUpdate(mViewTypes[(int)viewTypeChoice]);

                    if (Application.isPlaying)
                        EditorGUI.EndDisabledGroup();
                }

                /// <summary>
                /// Add 'PhotonRigidbodyView' or 'PhotonRigidbody2DView' to gameObject 'PhotonView' components
                /// </summary>
                /// <typeparam name="T">generic 'T' must be 'PhotonRigidbodyView' or 'PhotonRigidbody2DView'.</typeparam>
                private void AddPhotonViewComponents<T>() where T : MonoBehaviour, IPunObservable 
                {
                    // Type check
                    Type type = typeof(T);
                    if (!type.Equals(typeof(PhotonRigidbodyView)) && 
                        !type.Equals(typeof(PhotonRigidbody2DView)))
                        return;

                    T photonRigidbodyView = noLagView.GetComponent<T>();
                    if (photonRigidbodyView == null)
                    {
                        photonRigidbodyView = noLagView.gameObject.AddComponent<T>();
                        noLagView.photonView.ObservedComponents.Add(photonRigidbodyView);
                    }

                    else
                    {
                        var observedCompo = noLagView.photonView.ObservedComponents.Find(observedRigidbodyView =>
                            photonRigidbodyView == observedRigidbodyView
                        );

                        if (observedCompo == null)
                            noLagView.photonView.ObservedComponents.Add(photonRigidbodyView);
                    }
                    
                    //T observedRigidbodyView = null;
                    //foreach (var rigidbodyView in noLagView.photonView.ObservedComponents.OfType<T>())
                    //    observedRigidbodyView = rigidbodyView;

                    //if (observedRigidbodyView == null)
                    //{
                    //    noLagView.photonView.ObservedComponents.Add(photonRigidbodyView);    
                    //}
                }
            }
        }
    }
}