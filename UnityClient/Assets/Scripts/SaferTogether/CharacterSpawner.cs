using UnityEngine;

namespace SaferTogether.UnityClient
{
    // instantiates a single pack character prefab under a mount point and exposes its Animator so
    // callers can play idle/walk. shared by the avatar editor preview and the mission room.
    public sealed class CharacterSpawner : MonoBehaviour
    {
        public Transform mountPoint;

        private string currentCharacter = "";
        private GameObject currentInstance;
        private Animator currentAnimator;

        public GameObject CurrentInstance => currentInstance;
        public Animator CurrentAnimator => currentAnimator;
        public string CurrentCharacter => currentCharacter;

        // show the character for an avatar id ("pack:swat") or a bare character id ("swat").
        // returns the instance, or null when the prefab is missing.
        public GameObject Show(string avatarOrCharacterId)
        {
            string character = AvatarCatalog.ResolveCharacter(avatarOrCharacterId);

            if (character == currentCharacter && currentInstance != null)
            {
                return currentInstance;
            }

            Clear();

            GameObject prefab = AvatarCatalog.LoadPrefab(character);

            if (prefab == null)
            {
                Debug.LogWarning("[CharacterSpawner] Prefab not found for character: " + character);
                return null;
            }

            Transform parent = mountPoint != null ? mountPoint : transform;
            currentInstance = Instantiate(prefab, parent);
            currentInstance.name = character;
            currentInstance.transform.localPosition = Vector3.zero;
            currentInstance.transform.localRotation = Quaternion.identity;
            currentInstance.transform.localScale = Vector3.one;
            currentAnimator = currentInstance.GetComponentInChildren<Animator>();
            currentCharacter = character;
            return currentInstance;
        }

        // play idle (false) or walk (true) if the character has the shared controller
        public void SetWalking(bool walking)
        {
            if (currentAnimator != null && currentAnimator.runtimeAnimatorController != null)
            {
                currentAnimator.SetBool(AvatarCatalog.WalkingParam, walking);
            }
        }

        // remove the current character instance
        public void Clear()
        {
            if (currentInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(currentInstance);
                }
                else
                {
                    DestroyImmediate(currentInstance);
                }
            }

            currentInstance = null;
            currentAnimator = null;
            currentCharacter = "";
        }
    }
}
