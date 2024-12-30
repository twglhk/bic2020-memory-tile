
namespace Game
{
    namespace Systems
    {
        using System.Collections;
        using System.Collections.Generic;
        using UnityEngine;
        using UnityEngine.UI;
        using Game.Server;
        using Game.DB;

        [UnitySingleton(UnitySingletonAttribute.Type.ExistsInScene)]
        public class LobbyManager : UnitySingleton<LobbyManager>
        {
            public Button StartButton;
            public AudioSource ButtonClickSound;
            public SpriteRenderer[] titleResources;
            public Text loadingText;
            public Text ReadyText;

            // Start is called before the first frame update
            void Start()
            {
                GameData.Instance.LobbyBgm.Play();

                StartButton.onClick.AddListener(() => {
                    ButtonClickSound.Play();
                    PhotonManager.Instance.LeaveLobby();
                    ReadyText.text = "Matching..";
                    StartButton.gameObject.SetActive(false);
                });
            }

            public void FadeOutTitle()
            {
                StartCoroutine(FadeOutRoutine());
            }
            
            private IEnumerator FadeOutRoutine()
            {
                yield return null;
                yield return new WaitForSeconds(3f);

                loadingText.enabled = false;
                float alpha = 1f;
                float alphaMinus = 0.01f;
                while (alpha > 0f)
                {
                    alpha -= alphaMinus;
                    foreach(var resource in titleResources)
                    {
                        resource.color = new Color(resource.color.r, resource.color.g, resource.color.b, alpha);
                    }
                    yield return null;
                }
            }
        }
    }
}
