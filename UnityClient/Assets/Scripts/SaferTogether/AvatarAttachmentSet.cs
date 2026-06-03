using UnityEngine;

namespace SaferTogether.UnityClient
{
    public enum AvatarAttachmentSlot
    {
        Face,
        Hat,
        Horns,
        Wings,
        Tail,
        Shirt,
        Pants,
        Shoes,
        LeftShoe,
        RightShoe
    }

    /// <summary>
    /// Defines the body points that avatar parts should attach to on one avatar prefab.
    /// </summary>
    public sealed class AvatarAttachmentSet : MonoBehaviour
    {
        [Header("Accessory Points")]
        public Transform facePoint;
        public Transform hatPoint;
        public Transform hornsPoint;
        public Transform wingsPoint;
        public Transform tailPoint;

        [Header("Clothing Points")]
        public Transform shirtPoint;
        public Transform pantsPoint;
        public Transform shoesPoint;
        public Transform leftShoePoint;
        public Transform rightShoePoint;

        public Transform PointFor(AvatarAttachmentSlot slot)
        {
            switch (slot)
            {
                case AvatarAttachmentSlot.Face:
                    return facePoint;
                case AvatarAttachmentSlot.Hat:
                    return hatPoint;
                case AvatarAttachmentSlot.Horns:
                    return hornsPoint;
                case AvatarAttachmentSlot.Wings:
                    return wingsPoint;
                case AvatarAttachmentSlot.Tail:
                    return tailPoint;
                case AvatarAttachmentSlot.Shirt:
                    return shirtPoint;
                case AvatarAttachmentSlot.Pants:
                    return pantsPoint;
                case AvatarAttachmentSlot.Shoes:
                    return shoesPoint != null ? shoesPoint : transform;
                case AvatarAttachmentSlot.LeftShoe:
                    return leftShoePoint;
                case AvatarAttachmentSlot.RightShoe:
                    return rightShoePoint;
                default:
                    return transform;
            }
        }
    }
}
