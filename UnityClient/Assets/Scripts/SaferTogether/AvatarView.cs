using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    /// <summary>
    /// Displays the selected avatar as a layered character preview.
    /// </summary>
    public sealed class AvatarView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image artImage;
        [SerializeField] private Image wingsImage;
        [SerializeField] private Image tailImage;
        [SerializeField] private Image leftArmImage;
        [SerializeField] private Image rightArmImage;
        [SerializeField] private Image leftHandImage;
        [SerializeField] private Image rightHandImage;
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image topCenterDetailImage;
        [SerializeField] private Image leftTopDetailImage;
        [SerializeField] private Image rightTopDetailImage;
        [SerializeField] private Image neckImage;
        [SerializeField] private Image bottomImage;
        [SerializeField] private Image waistDetailImage;
        [SerializeField] private Image leftBottomDetailImage;
        [SerializeField] private Image rightBottomDetailImage;
        [SerializeField] private Image leftLegImage;
        [SerializeField] private Image rightLegImage;
        [SerializeField] private Image leftShoeImage;
        [SerializeField] private Image rightShoeImage;
        [SerializeField] private Image leftShoeDetailImage;
        [SerializeField] private Image rightShoeDetailImage;
        [SerializeField] private Image leftEarImage;
        [SerializeField] private Image rightEarImage;
        [SerializeField] private Image headImage;
        [SerializeField] private Image backHairImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image leftHairDetailImage;
        [SerializeField] private Image rightHairDetailImage;
        [SerializeField] private Image jawShadowImage;
        [SerializeField] private Image leftCheekImage;
        [SerializeField] private Image rightCheekImage;
        [SerializeField] private Image trunkImage;
        [SerializeField] private Image leftEyeWhiteImage;
        [SerializeField] private Image rightEyeWhiteImage;
        [SerializeField] private Image leftEyeImage;
        [SerializeField] private Image rightEyeImage;
        [SerializeField] private Image leftBrowImage;
        [SerializeField] private Image rightBrowImage;
        [SerializeField] private Image mouthImage;
        [SerializeField] private Image accessoryImage;
        [SerializeField] private Image accessoryLeftDetailImage;
        [SerializeField] private Image accessoryRightDetailImage;
        [SerializeField] private Text badgeLabel;

        private static Sprite circleSprite;
        private static Sprite roundedSprite;
        private static Sprite diamondSprite;
        private static Sprite faceSprite;
        private static Sprite longFaceSprite;
        private static Sprite hairCapSprite;
        private static Sprite spikyHairSprite;
        private static Sprite torsoSprite;
        private static Sprite legSprite;
        private static Sprite shoeSprite;
        private static Sprite skirtSprite;
        private static readonly Dictionary<string, Sprite> artSpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// This function assigns the UI references when the character is created in code.
        /// </summary>
        public void Bind(
            Image background,
            Image art,
            Image wings,
            Image tail,
            Image leftArm,
            Image rightArm,
            Image leftHand,
            Image rightHand,
            Image body,
            Image topCenterDetail,
            Image leftTopDetail,
            Image rightTopDetail,
            Image neck,
            Image bottom,
            Image waistDetail,
            Image leftBottomDetail,
            Image rightBottomDetail,
            Image leftLeg,
            Image rightLeg,
            Image leftShoe,
            Image rightShoe,
            Image leftShoeDetail,
            Image rightShoeDetail,
            Image leftEar,
            Image rightEar,
            Image head,
            Image backHair,
            Image hair,
            Image leftHairDetail,
            Image rightHairDetail,
            Image jawShadow,
            Image leftCheek,
            Image rightCheek,
            Image trunk,
            Image leftEyeWhite,
            Image rightEyeWhite,
            Image leftEye,
            Image rightEye,
            Image leftBrow,
            Image rightBrow,
            Image mouth,
            Image accessory,
            Image accessoryLeftDetail,
            Image accessoryRightDetail,
            Text badge
        )
        {
            backgroundImage = background;
            artImage = art;
            wingsImage = wings;
            tailImage = tail;
            leftArmImage = leftArm;
            rightArmImage = rightArm;
            leftHandImage = leftHand;
            rightHandImage = rightHand;
            bodyImage = body;
            topCenterDetailImage = topCenterDetail;
            leftTopDetailImage = leftTopDetail;
            rightTopDetailImage = rightTopDetail;
            neckImage = neck;
            bottomImage = bottom;
            waistDetailImage = waistDetail;
            leftBottomDetailImage = leftBottomDetail;
            rightBottomDetailImage = rightBottomDetail;
            leftLegImage = leftLeg;
            rightLegImage = rightLeg;
            leftShoeImage = leftShoe;
            rightShoeImage = rightShoe;
            leftShoeDetailImage = leftShoeDetail;
            rightShoeDetailImage = rightShoeDetail;
            leftEarImage = leftEar;
            rightEarImage = rightEar;
            headImage = head;
            backHairImage = backHair;
            hairImage = hair;
            leftHairDetailImage = leftHairDetail;
            rightHairDetailImage = rightHairDetail;
            jawShadowImage = jawShadow;
            leftCheekImage = leftCheek;
            rightCheekImage = rightCheek;
            trunkImage = trunk;
            leftEyeWhiteImage = leftEyeWhite;
            rightEyeWhiteImage = rightEyeWhite;
            leftEyeImage = leftEye;
            rightEyeImage = rightEye;
            leftBrowImage = leftBrow;
            rightBrowImage = rightBrow;
            mouthImage = mouth;
            accessoryImage = accessory;
            accessoryLeftDetailImage = accessoryLeftDetail;
            accessoryRightDetailImage = accessoryRightDetail;
            badgeLabel = badge;
            ApplySprites();
        }

        /// <summary>
        /// This function updates the avatar character from a saved avatar id.
        /// </summary>
        public void SetAvatar(string username, string avatar)
        {
            CharacterAvatarSpec spec = CharacterAvatarId.ToSpec(avatar);
            Color skinColor = SkinColor(spec.skin);
            Color hairColor = HairColor(spec.hairColor);
            Color topColor = ColorForAvatar(spec.topColor);
            Color bottomColor = ColorForAvatar(spec.bottomColor);
            Color shoeColor = ColorForAvatar(spec.shoeColor);
            Color backgroundColor = ColorForAvatar(spec.background);

            SetImageColor(backgroundImage, backgroundColor);
            SetImageColor(leftArmImage, topColor);
            SetImageColor(rightArmImage, topColor);
            SetImageColor(leftHandImage, skinColor);
            SetImageColor(rightHandImage, skinColor);
            SetImageColor(bodyImage, topColor);
            SetImageColor(neckImage, skinColor);
            SetImageColor(headImage, skinColor);
            SetImageColor(leftEarImage, skinColor);
            SetImageColor(rightEarImage, skinColor);
            SetImageColor(trunkImage, skinColor);
            SetImageColor(backHairImage, hairColor);
            SetImageColor(hairImage, hairColor);
            SetImageColor(leftHairDetailImage, Darken(hairColor, 0.72f));
            SetImageColor(rightHairDetailImage, Darken(hairColor, 0.72f));
            SetImageColor(jawShadowImage, Darken(skinColor, 0.82f));
            SetImageColor(leftCheekImage, CheekColorForSkin(skinColor));
            SetImageColor(rightCheekImage, CheekColorForSkin(skinColor));
            SetImageColor(bottomImage, bottomColor);
            SetImageColor(leftLegImage, bottomColor);
            SetImageColor(rightLegImage, bottomColor);
            SetImageColor(leftShoeImage, shoeColor);
            SetImageColor(rightShoeImage, shoeColor);
            SetImageColor(leftEyeWhiteImage, new Color32(250, 253, 255, 255));
            SetImageColor(rightEyeWhiteImage, new Color32(250, 253, 255, 255));
            SetImageColor(leftEyeImage, EyeColor(spec.eyeColor));
            SetImageColor(rightEyeImage, EyeColor(spec.eyeColor));
            SetImageColor(leftBrowImage, Darken(hairColor, 0.72f));
            SetImageColor(rightBrowImage, Darken(hairColor, 0.72f));
            SetImageColor(mouthImage, ColorForFaceDetail(spec.skin));
            SetImageColor(accessoryImage, AccessoryColor(spec.accessory));
            SetImageColor(accessoryLeftDetailImage, AccessoryColor(spec.accessory));
            SetImageColor(accessoryRightDetailImage, AccessoryColor(spec.accessory));
            SetText(badgeLabel, InitialFor(username), ContrastColor(spec.topColor));

            if (ApplyIllustratedAvatar(spec.species))
            {
                ApplyIllustratedOverlays(spec, topColor, bottomColor, shoeColor);
                return;
            }

            SetProceduralLayersVisible(true);
            ApplySpeciesStyle(spec.species, spec.accessory, skinColor);
            ApplyFaceStyle(spec.face, spec.species, skinColor);
            ApplyHairStyle(spec.hair);
            ApplyTopStyle(spec.top, skinColor, topColor);
            ApplyBottomStyle(spec.bottom, spec.top, skinColor, bottomColor);
            ApplyShoesStyle(spec.shoes);
            ApplyEyesStyle(spec.eyes);
            ApplyMouthStyle(spec.face, spec.species);
            ApplyAccessoryStyle(spec.accessory);
            ApplyGenderStyle(spec.sex, spec.species);
        }

        /// <summary>
        /// This function assigns generated sprites to image layers.
        /// </summary>
        private void ApplySprites()
        {
            SetSprite(backgroundImage, RoundedSprite());
            SetSprite(artImage, RoundedSprite());
            SetSprite(wingsImage, DiamondSprite());
            SetSprite(tailImage, RoundedSprite());
            SetSprite(leftArmImage, RoundedSprite());
            SetSprite(rightArmImage, RoundedSprite());
            SetSprite(leftHandImage, CircleSprite());
            SetSprite(rightHandImage, CircleSprite());
            SetSprite(bodyImage, RoundedSprite());
            SetSprite(topCenterDetailImage, RoundedSprite());
            SetSprite(leftTopDetailImage, RoundedSprite());
            SetSprite(rightTopDetailImage, RoundedSprite());
            SetSprite(neckImage, RoundedSprite());
            SetSprite(bottomImage, RoundedSprite());
            SetSprite(waistDetailImage, RoundedSprite());
            SetSprite(leftBottomDetailImage, RoundedSprite());
            SetSprite(rightBottomDetailImage, RoundedSprite());
            SetSprite(leftLegImage, RoundedSprite());
            SetSprite(rightLegImage, RoundedSprite());
            SetSprite(leftShoeImage, RoundedSprite());
            SetSprite(rightShoeImage, RoundedSprite());
            SetSprite(leftShoeDetailImage, RoundedSprite());
            SetSprite(rightShoeDetailImage, RoundedSprite());
            SetSprite(leftEarImage, CircleSprite());
            SetSprite(rightEarImage, CircleSprite());
            SetSprite(headImage, CircleSprite());
            SetSprite(backHairImage, RoundedSprite());
            SetSprite(hairImage, RoundedSprite());
            SetSprite(leftHairDetailImage, RoundedSprite());
            SetSprite(rightHairDetailImage, RoundedSprite());
            SetSprite(jawShadowImage, RoundedSprite());
            SetSprite(leftCheekImage, CircleSprite());
            SetSprite(rightCheekImage, CircleSprite());
            SetSprite(trunkImage, RoundedSprite());
            SetSprite(leftEyeWhiteImage, RoundedSprite());
            SetSprite(rightEyeWhiteImage, RoundedSprite());
            SetSprite(leftEyeImage, CircleSprite());
            SetSprite(rightEyeImage, CircleSprite());
            SetSprite(leftBrowImage, RoundedSprite());
            SetSprite(rightBrowImage, RoundedSprite());
            SetSprite(mouthImage, RoundedSprite());
            SetSprite(accessoryImage, RoundedSprite());
            SetSprite(accessoryLeftDetailImage, RoundedSprite());
            SetSprite(accessoryRightDetailImage, RoundedSprite());
        }

        /// <summary>
        /// This function applies creature-specific parts.
        /// </summary>
        private static bool IsHumanAvatarSpecies(string species)
        {
            return species == CharacterAvatarOptions.Male
                || species == CharacterAvatarOptions.Female
                || species == CharacterAvatarOptions.Human;
        }

        private void ApplySpeciesStyle(string species, string accessory, Color skinColor)
        {
            string value = CharacterAvatarId.NormalizeSpecies(species);
            string accessoryValue = CharacterAvatarId.NormalizeAccessory(accessory);
            bool hasWings = value == CharacterAvatarOptions.Angel
                || value == CharacterAvatarOptions.Dragon
                || accessoryValue == CharacterAvatarOptions.Wings;
            bool hasTail = value == CharacterAvatarOptions.Dragon
                || value == CharacterAvatarOptions.Devil
                || accessoryValue == CharacterAvatarOptions.Tail;
            bool hasRoundEars = value == CharacterAvatarOptions.Bear
                || value == CharacterAvatarOptions.Elephant;
            bool hasHumanEars = IsHumanAvatarSpecies(value)
                || value == CharacterAvatarOptions.Angel;
            bool hasHorns = value == CharacterAvatarOptions.Dragon
                || value == CharacterAvatarOptions.Devil
                || accessoryValue == CharacterAvatarOptions.Horns;

            SetImageVisible(wingsImage, hasWings);
            SetImageVisible(tailImage, hasTail);
            SetImageVisible(leftEarImage, hasHumanEars || hasRoundEars || hasHorns);
            SetImageVisible(rightEarImage, hasHumanEars || hasRoundEars || hasHorns);

            if (wingsImage != null)
            {
                wingsImage.sprite = value == CharacterAvatarOptions.Dragon ? DiamondSprite() : RoundedSprite();
                wingsImage.color = value == CharacterAvatarOptions.Dragon
                    ? Darken(skinColor, 0.82f)
                    : new Color32(248, 250, 252, 220);
                wingsImage.rectTransform.anchoredPosition = new Vector2(0, -24);
                wingsImage.rectTransform.sizeDelta = new Vector2(230, 118);
            }

            if (tailImage != null)
            {
                tailImage.color = value == CharacterAvatarOptions.Devil
                    ? new Color32(178, 55, 55, 255)
                    : Darken(skinColor, 0.78f);
                tailImage.rectTransform.anchoredPosition = new Vector2(74, -118);
                tailImage.rectTransform.sizeDelta = new Vector2(64, 12);
                tailImage.rectTransform.localEulerAngles = new Vector3(0, 0, -18f);
            }

            ApplyEarOrHornStyle(value, hasHorns, skinColor);
        }

        /// <summary>
        /// This function styles ears and horns that share the same side layers.
        /// </summary>
        private void ApplyEarOrHornStyle(string species, bool horns, Color skinColor)
        {
            if (leftEarImage == null || rightEarImage == null)
            {
                return;
            }

            leftEarImage.sprite = CircleSprite();
            rightEarImage.sprite = CircleSprite();
            leftEarImage.color = skinColor;
            rightEarImage.color = skinColor;
            leftEarImage.rectTransform.anchoredPosition = new Vector2(-58, 76);
            rightEarImage.rectTransform.anchoredPosition = new Vector2(58, 76);
            leftEarImage.rectTransform.sizeDelta = new Vector2(34, 42);
            rightEarImage.rectTransform.sizeDelta = new Vector2(34, 42);

            if (species == CharacterAvatarOptions.Elephant)
            {
                leftEarImage.rectTransform.anchoredPosition = new Vector2(-72, 66);
                rightEarImage.rectTransform.anchoredPosition = new Vector2(72, 66);
                leftEarImage.rectTransform.sizeDelta = new Vector2(58, 82);
                rightEarImage.rectTransform.sizeDelta = new Vector2(58, 82);
            }

            if (horns)
            {
                leftEarImage.sprite = DiamondSprite();
                rightEarImage.sprite = DiamondSprite();
                leftEarImage.color = new Color32(178, 55, 55, 255);
                rightEarImage.color = new Color32(178, 55, 55, 255);
                leftEarImage.rectTransform.anchoredPosition = new Vector2(-35, 142);
                rightEarImage.rectTransform.anchoredPosition = new Vector2(35, 142);
                leftEarImage.rectTransform.sizeDelta = new Vector2(20, 32);
                rightEarImage.rectTransform.sizeDelta = new Vector2(20, 32);
            }
        }

        /// <summary>
        /// This function applies the selected face shape.
        /// </summary>
        private void ApplyFaceStyle(string face, string species, Color skinColor)
        {
            if (headImage == null || trunkImage == null)
            {
                return;
            }

            string value = CharacterAvatarId.NormalizeFace(face);
            string speciesValue = CharacterAvatarId.NormalizeSpecies(species);
            RectTransform headRect = headImage.rectTransform;
            headImage.sprite = FaceSprite();
            headRect.anchoredPosition = new Vector2(0, 78);
            headRect.sizeDelta = new Vector2(92, 108);
            ApplyFaceDetailLayers(value, speciesValue, skinColor);

            if (neckImage != null)
            {
                neckImage.enabled = true;
                neckImage.rectTransform.anchoredPosition = new Vector2(0, 18);
                neckImage.rectTransform.sizeDelta = new Vector2(34, 44);
            }

            if (value == CharacterAvatarOptions.Soft)
            {
                headImage.sprite = FaceSprite();
            }
            else if (value == CharacterAvatarOptions.Round)
            {
                headImage.sprite = CircleSprite();
                headRect.sizeDelta = new Vector2(100, 104);
            }
            else if (value == CharacterAvatarOptions.Sharp)
            {
                headImage.sprite = FaceSprite();
                headRect.sizeDelta = new Vector2(86, 116);
            }
            else if (value == CharacterAvatarOptions.LongFace)
            {
                headImage.sprite = LongFaceSprite();
                headRect.sizeDelta = new Vector2(82, 124);
                headRect.anchoredPosition = new Vector2(0, 74);
            }

            bool showSnout = value == CharacterAvatarOptions.Snout || speciesValue == CharacterAvatarOptions.Elephant;
            trunkImage.enabled = showSnout;
            trunkImage.color = skinColor;

            if (showSnout)
            {
                trunkImage.rectTransform.anchoredPosition = speciesValue == CharacterAvatarOptions.Elephant
                    ? new Vector2(0, 42)
                    : new Vector2(0, 64);
                trunkImage.rectTransform.sizeDelta = speciesValue == CharacterAvatarOptions.Elephant
                    ? new Vector2(34, 82)
                    : new Vector2(40, 28);
                return;
            }

            bool showNose = IsHumanAvatarSpecies(speciesValue)
                || speciesValue == CharacterAvatarOptions.Angel
                || speciesValue == CharacterAvatarOptions.Devil;
            trunkImage.enabled = showNose;

            if (showNose)
            {
                trunkImage.color = Darken(skinColor, 0.9f);
                trunkImage.sprite = RoundedSprite();
                trunkImage.rectTransform.anchoredPosition = new Vector2(0, 74);
                trunkImage.rectTransform.sizeDelta = new Vector2(10, 20);
            }
        }

        /// <summary>
        /// This function applies the selected hair shape to the hair layer.
        /// </summary>
        private void ApplyHairStyle(string hair)
        {
            if (hairImage == null || backHairImage == null)
            {
                return;
            }

            RectTransform frontRect = hairImage.rectTransform;
            RectTransform backRect = backHairImage.rectTransform;
            string value = CharacterAvatarId.NormalizeHair(hair);
            hairImage.enabled = value != CharacterAvatarOptions.NoHair;
            backHairImage.enabled = false;
            HideHairDetails();
            hairImage.sprite = HairCapSprite();
            backHairImage.sprite = RoundedSprite();
            frontRect.localEulerAngles = Vector3.zero;
            backRect.localEulerAngles = Vector3.zero;
            frontRect.anchoredPosition = new Vector2(0, 130);
            frontRect.sizeDelta = new Vector2(98, 44);
            backRect.anchoredPosition = new Vector2(0, 82);
            backRect.sizeDelta = new Vector2(112, 116);

            if (value == CharacterAvatarOptions.Bob || value == CharacterAvatarOptions.LongHair)
            {
                backHairImage.enabled = true;
                backRect.anchoredPosition = new Vector2(0, value == CharacterAvatarOptions.LongHair ? 48 : 68);
                backRect.sizeDelta = new Vector2(108, value == CharacterAvatarOptions.LongHair ? 168 : 126);
                frontRect.anchoredPosition = new Vector2(0, 128);
                frontRect.sizeDelta = new Vector2(104, 46);
                ConfigureDetail(leftHairDetailImage, RoundedSprite(), Darken(hairImage.color, 0.78f), new Vector2(-47, 78), new Vector2(18, value == CharacterAvatarOptions.LongHair ? 92 : 58));
                ConfigureDetail(rightHairDetailImage, RoundedSprite(), Darken(hairImage.color, 0.78f), new Vector2(47, 78), new Vector2(18, value == CharacterAvatarOptions.LongHair ? 92 : 58));
            }
            else if (value == CharacterAvatarOptions.Curls || value == CharacterAvatarOptions.Bun)
            {
                backHairImage.enabled = value == CharacterAvatarOptions.Bun;
                backHairImage.sprite = CircleSprite();
                hairImage.sprite = CircleSprite();
                backRect.anchoredPosition = new Vector2(0, 138);
                backRect.sizeDelta = new Vector2(46, 46);
                frontRect.anchoredPosition = new Vector2(0, 128);
                frontRect.sizeDelta = value == CharacterAvatarOptions.Bun
                    ? new Vector2(94, 34)
                    : new Vector2(118, 50);
                ConfigureDetail(leftHairDetailImage, CircleSprite(), Darken(hairImage.color, 0.82f), new Vector2(-43, 124), new Vector2(34, 34));
                ConfigureDetail(rightHairDetailImage, CircleSprite(), Darken(hairImage.color, 0.82f), new Vector2(43, 124), new Vector2(34, 34));
            }
            else if (value == CharacterAvatarOptions.Spiky || value == CharacterAvatarOptions.Mohawk)
            {
                backHairImage.enabled = value == CharacterAvatarOptions.Spiky;
                backHairImage.sprite = SpikyHairSprite();
                backRect.anchoredPosition = new Vector2(0, 148);
                backRect.sizeDelta = new Vector2(94, 58);
                frontRect.anchoredPosition = new Vector2(0, value == CharacterAvatarOptions.Mohawk ? 142 : 132);
                frontRect.sizeDelta = new Vector2(value == CharacterAvatarOptions.Mohawk ? 38 : 106, value == CharacterAvatarOptions.Mohawk ? 78 : 54);
                hairImage.sprite = value == CharacterAvatarOptions.Mohawk ? DiamondSprite() : SpikyHairSprite();
                if (value == CharacterAvatarOptions.Spiky)
                {
                    ConfigureDetail(leftHairDetailImage, DiamondSprite(), Darken(hairImage.color, 0.8f), new Vector2(-38, 132), new Vector2(28, 36), -16f);
                    ConfigureDetail(rightHairDetailImage, DiamondSprite(), Darken(hairImage.color, 0.8f), new Vector2(38, 132), new Vector2(28, 36), 16f);
                }
            }
            else if (value == CharacterAvatarOptions.Ponytail)
            {
                backHairImage.enabled = true;
                backHairImage.sprite = CircleSprite();
                backRect.anchoredPosition = new Vector2(52, 66);
                backRect.sizeDelta = new Vector2(54, 96);
                frontRect.anchoredPosition = new Vector2(0, 130);
                frontRect.sizeDelta = new Vector2(98, 42);
                ConfigureDetail(leftHairDetailImage, RoundedSprite(), Darken(hairImage.color, 0.78f), new Vector2(-42, 89), new Vector2(15, 42));
            }
            else if (value == CharacterAvatarOptions.Hijab)
            {
                backHairImage.enabled = true;
                backHairImage.sprite = RoundedSprite();
                backRect.anchoredPosition = new Vector2(0, 66);
                backRect.sizeDelta = new Vector2(118, 150);
                frontRect.anchoredPosition = new Vector2(0, 100);
                frontRect.sizeDelta = new Vector2(106, 86);
                hairImage.sprite = CircleSprite();
            }
        }

        /// <summary>
        /// This function applies the selected shirt/top style to the body layer.
        /// </summary>
        private void ApplyTopStyle(string top, Color skinColor, Color topColor)
        {
            if (bodyImage == null)
            {
                return;
            }

            RectTransform rect = bodyImage.rectTransform;
            string value = CharacterAvatarId.NormalizeTop(top);
            HideTopDetails();
            bodyImage.sprite = TorsoSprite();
            bodyImage.color = topColor;
            rect.anchoredPosition = new Vector2(0, -42);
            rect.sizeDelta = new Vector2(94, 98);

            PositionLimb(leftArmImage, new Vector2(-62, -44), new Vector2(20, 86), -12f);
            PositionLimb(rightArmImage, new Vector2(62, -44), new Vector2(20, 86), 12f);
            PositionLimb(leftHandImage, new Vector2(-77, -91), new Vector2(23, 23), 0f);
            PositionLimb(rightHandImage, new Vector2(77, -91), new Vector2(23, 23), 0f);
            SetSprite(leftHandImage, CircleSprite());
            SetSprite(rightHandImage, CircleSprite());
            SetImageColor(leftArmImage, topColor);
            SetImageColor(rightArmImage, topColor);
            SetImageColor(leftHandImage, skinColor);
            SetImageColor(rightHandImage, skinColor);

            if (value == CharacterAvatarOptions.Tee)
            {
                ConfigureDetail(topCenterDetailImage, DiamondSprite(), Lighten(topColor, 0.42f), new Vector2(0, 6), new Vector2(34, 18));
                ConfigureDetail(leftTopDetailImage, RoundedSprite(), Lighten(topColor, 0.2f), new Vector2(-45, -8), new Vector2(16, 36), -12f);
                ConfigureDetail(rightTopDetailImage, RoundedSprite(), Lighten(topColor, 0.2f), new Vector2(45, -8), new Vector2(16, 36), 12f);
            }
            else if (value == CharacterAvatarOptions.Shirt)
            {
                rect.sizeDelta = new Vector2(96, 98);
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), Darken(topColor, 0.58f), new Vector2(0, -34), new Vector2(5, 72));
                ConfigureDetail(leftTopDetailImage, DiamondSprite(), Lighten(topColor, 0.68f), new Vector2(-19, 2), new Vector2(34, 30), -8f);
                ConfigureDetail(rightTopDetailImage, DiamondSprite(), Lighten(topColor, 0.68f), new Vector2(19, 2), new Vector2(34, 30), 8f);
            }
            else if (value == CharacterAvatarOptions.Hoodie)
            {
                rect.sizeDelta = new Vector2(106, 102);
                PositionLimb(leftArmImage, new Vector2(-68, -45), new Vector2(25, 92), -9f);
                PositionLimb(rightArmImage, new Vector2(68, -45), new Vector2(25, 92), 9f);
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), Darken(topColor, 0.78f), new Vector2(0, -66), new Vector2(60, 21));
                ConfigureDetail(leftTopDetailImage, RoundedSprite(), Lighten(topColor, 0.62f), new Vector2(-11, -8), new Vector2(4, 42), -9f);
                ConfigureDetail(rightTopDetailImage, RoundedSprite(), Lighten(topColor, 0.62f), new Vector2(11, -8), new Vector2(4, 42), 9f);
            }
            else if (value == CharacterAvatarOptions.Sweatshirt)
            {
                rect.sizeDelta = new Vector2(104, 98);
                PositionLimb(leftArmImage, new Vector2(-67, -44), new Vector2(25, 90), -10f);
                PositionLimb(rightArmImage, new Vector2(67, -44), new Vector2(25, 90), 10f);
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), Lighten(topColor, 0.24f), new Vector2(0, -20), new Vector2(72, 13));
            }
            else if (value == CharacterAvatarOptions.Jacket)
            {
                rect.sizeDelta = new Vector2(112, 98);
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), Lighten(topColor, 0.72f), new Vector2(0, -36), new Vector2(5, 76));
                ConfigureDetail(leftTopDetailImage, DiamondSprite(), Darken(topColor, 0.62f), new Vector2(-25, -18), new Vector2(42, 74), -5f);
                ConfigureDetail(rightTopDetailImage, DiamondSprite(), Darken(topColor, 0.62f), new Vector2(25, -18), new Vector2(42, 74), 5f);
            }
            else if (value == CharacterAvatarOptions.Armor)
            {
                rect.sizeDelta = new Vector2(124, 104);
                bodyImage.sprite = DiamondSprite();
                ConfigureDetail(topCenterDetailImage, DiamondSprite(), Lighten(topColor, 0.42f), new Vector2(0, -26), new Vector2(64, 72));
                ConfigureDetail(leftTopDetailImage, DiamondSprite(), Darken(topColor, 0.68f), new Vector2(-54, -18), new Vector2(42, 42), -8f);
                ConfigureDetail(rightTopDetailImage, DiamondSprite(), Darken(topColor, 0.68f), new Vector2(54, -18), new Vector2(42, 42), 8f);
            }
            else if (value == CharacterAvatarOptions.Vest)
            {
                rect.sizeDelta = new Vector2(90, 88);
                SetImageColor(leftArmImage, skinColor);
                SetImageColor(rightArmImage, skinColor);
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), Darken(topColor, 0.62f), new Vector2(0, -30), new Vector2(5, 62));
                ConfigureDetail(leftTopDetailImage, DiamondSprite(), Lighten(topColor, 0.25f), new Vector2(-18, -9), new Vector2(26, 48), -6f);
                ConfigureDetail(rightTopDetailImage, DiamondSprite(), Lighten(topColor, 0.25f), new Vector2(18, -9), new Vector2(26, 48), 6f);
            }
            else if (value == CharacterAvatarOptions.Dress)
            {
                rect.anchoredPosition = new Vector2(0, -42);
                rect.sizeDelta = new Vector2(108, 116);
                bodyImage.sprite = SkirtSprite();
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), Lighten(topColor, 0.35f), new Vector2(0, -2), new Vector2(44, 10));
            }
        }

        /// <summary>
        /// This function applies the selected pants/bottom style.
        /// </summary>
        private void ApplyBottomStyle(string bottom, string top, Color skinColor, Color bottomColor)
        {
            if (bottomImage == null)
            {
                return;
            }

            RectTransform rect = bottomImage.rectTransform;
            string value = CharacterAvatarId.NormalizeBottom(bottom);
            string topValue = CharacterAvatarId.NormalizeTop(top);
            HideBottomDetails();
            bottomImage.sprite = RoundedSprite();
            bottomImage.enabled = true;
            rect.anchoredPosition = new Vector2(0, -92);
            rect.sizeDelta = new Vector2(80, 34);
            PositionLimb(leftLegImage, new Vector2(-22, -128), new Vector2(24, 80), 0f);
            PositionLimb(rightLegImage, new Vector2(22, -128), new Vector2(24, 80), 0f);
            SetSprite(leftLegImage, LegSprite());
            SetSprite(rightLegImage, LegSprite());
            SetImageColor(bottomImage, bottomColor);
            SetImageColor(leftLegImage, bottomColor);
            SetImageColor(rightLegImage, bottomColor);

            if (topValue == CharacterAvatarOptions.Dress)
            {
                bottomImage.sprite = DiamondSprite();
                rect.anchoredPosition = new Vector2(0, -102);
                rect.sizeDelta = new Vector2(110, 92);
                SetImageColor(leftLegImage, skinColor);
                SetImageColor(rightLegImage, skinColor);
                PositionLimb(leftLegImage, new Vector2(-20, -148), new Vector2(24, 42), 0f);
                PositionLimb(rightLegImage, new Vector2(20, -148), new Vector2(24, 42), 0f);
                SetSprite(leftLegImage, RoundedSprite());
                SetSprite(rightLegImage, RoundedSprite());
                ConfigureDetail(waistDetailImage, RoundedSprite(), Darken(bottomColor, 0.76f), new Vector2(0, -72), new Vector2(84, 10));
                ConfigureDetail(leftBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.24f), new Vector2(-24, -124), new Vector2(6, 44));
                ConfigureDetail(rightBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.24f), new Vector2(24, -124), new Vector2(6, 44));
                return;
            }

            if (value == CharacterAvatarOptions.Shorts || value == CharacterAvatarOptions.Skirt)
            {
                rect.anchoredPosition = new Vector2(0, -96);
                rect.sizeDelta = new Vector2(92, value == CharacterAvatarOptions.Skirt ? 56 : 34);
                bottomImage.sprite = value == CharacterAvatarOptions.Skirt ? SkirtSprite() : RoundedSprite();
                SetImageColor(leftLegImage, skinColor);
                SetImageColor(rightLegImage, skinColor);
                PositionLimb(leftLegImage, new Vector2(-22, -140), new Vector2(24, 58), 0f);
                PositionLimb(rightLegImage, new Vector2(22, -140), new Vector2(24, 58), 0f);
                SetSprite(leftLegImage, RoundedSprite());
                SetSprite(rightLegImage, RoundedSprite());
                ConfigureDetail(waistDetailImage, RoundedSprite(), Darken(bottomColor, 0.74f), new Vector2(0, -76), new Vector2(84, 9));

                if (value == CharacterAvatarOptions.Shorts)
                {
                    ConfigureDetail(leftBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.28f), new Vector2(-28, -97), new Vector2(18, 20));
                    ConfigureDetail(rightBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.28f), new Vector2(28, -97), new Vector2(18, 20));
                }
            }
            else if (value == CharacterAvatarOptions.Cargo)
            {
                rect.sizeDelta = new Vector2(94, 42);
                PositionLimb(leftLegImage, new Vector2(-27, -126), new Vector2(32, 82), 0f);
                PositionLimb(rightLegImage, new Vector2(27, -126), new Vector2(32, 82), 0f);
                SetSprite(leftLegImage, LegSprite());
                SetSprite(rightLegImage, LegSprite());
                ConfigureDetail(waistDetailImage, RoundedSprite(), Darken(bottomColor, 0.66f), new Vector2(0, -75), new Vector2(88, 10));
                ConfigureDetail(leftBottomDetailImage, RoundedSprite(), Darken(bottomColor, 0.72f), new Vector2(-31, -125), new Vector2(20, 24));
                ConfigureDetail(rightBottomDetailImage, RoundedSprite(), Darken(bottomColor, 0.72f), new Vector2(31, -125), new Vector2(20, 24));
            }
            else if (value == CharacterAvatarOptions.Leggings)
            {
                rect.sizeDelta = new Vector2(70, 28);
                PositionLimb(leftLegImage, new Vector2(-18, -128), new Vector2(22, 80), 0f);
                PositionLimb(rightLegImage, new Vector2(18, -128), new Vector2(22, 80), 0f);
                SetSprite(leftLegImage, LegSprite());
                SetSprite(rightLegImage, LegSprite());
                ConfigureDetail(waistDetailImage, RoundedSprite(), Lighten(bottomColor, 0.25f), new Vector2(0, -78), new Vector2(72, 8));
                ConfigureDetail(leftBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.4f), new Vector2(-30, -128), new Vector2(5, 76));
                ConfigureDetail(rightBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.4f), new Vector2(30, -128), new Vector2(5, 76));
            }
            else if (value == CharacterAvatarOptions.Training)
            {
                rect.sizeDelta = new Vector2(80, 30);
                PositionLimb(leftLegImage, new Vector2(-23, -128), new Vector2(26, 82), 0f);
                PositionLimb(rightLegImage, new Vector2(23, -128), new Vector2(26, 82), 0f);
                SetSprite(leftLegImage, LegSprite());
                SetSprite(rightLegImage, LegSprite());
                ConfigureDetail(waistDetailImage, RoundedSprite(), Darken(bottomColor, 0.8f), new Vector2(0, -78), new Vector2(78, 8));
                ConfigureDetail(leftBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.5f), new Vector2(-36, -128), new Vector2(5, 80));
                ConfigureDetail(rightBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.5f), new Vector2(36, -128), new Vector2(5, 80));
            }
            else
            {
                ConfigureDetail(waistDetailImage, RoundedSprite(), Darken(bottomColor, 0.62f), new Vector2(0, -75), new Vector2(82, 10));
                ConfigureDetail(leftBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.25f), new Vector2(-25, -101), new Vector2(16, 18));
                ConfigureDetail(rightBottomDetailImage, RoundedSprite(), Lighten(bottomColor, 0.25f), new Vector2(25, -101), new Vector2(16, 18));
            }
        }

        /// <summary>
        /// This function applies the selected shoe style.
        /// </summary>
        private void ApplyShoesStyle(string shoes)
        {
            string value = CharacterAvatarId.NormalizeShoes(shoes);
            bool visible = value != CharacterAvatarOptions.NoShoes;
            SetImageVisible(leftShoeImage, visible);
            SetImageVisible(rightShoeImage, visible);
            SetImageVisible(leftShoeDetailImage, false);
            SetImageVisible(rightShoeDetailImage, false);

            if (!visible || leftShoeImage == null || rightShoeImage == null)
            {
                return;
            }

            Color shoeColor = leftShoeImage.color;
            leftShoeImage.sprite = ShoeSprite();
            rightShoeImage.sprite = ShoeSprite();
            leftShoeImage.rectTransform.localEulerAngles = Vector3.zero;
            rightShoeImage.rectTransform.localEulerAngles = Vector3.zero;
            Vector2 size = new Vector2(34, 18);
            Vector2 leftPosition = new Vector2(-22, -170);
            Vector2 rightPosition = new Vector2(22, -170);

            if (value == CharacterAvatarOptions.Boots)
            {
                size = new Vector2(34, 34);
                leftPosition = new Vector2(-22, -162);
                rightPosition = new Vector2(22, -162);
                SetImageColor(leftShoeImage, Darken(shoeColor, 0.82f));
                SetImageColor(rightShoeImage, Darken(shoeColor, 0.82f));
                ConfigureDetail(leftShoeDetailImage, RoundedSprite(), Lighten(shoeColor, 0.2f), new Vector2(-22, -153), new Vector2(24, 5));
                ConfigureDetail(rightShoeDetailImage, RoundedSprite(), Lighten(shoeColor, 0.2f), new Vector2(22, -153), new Vector2(24, 5));
            }
            else if (value == CharacterAvatarOptions.Sandals)
            {
                size = new Vector2(32, 9);
                leftPosition = new Vector2(-22, -174);
                rightPosition = new Vector2(22, -174);
                SetImageColor(leftShoeImage, Darken(shoeColor, 0.68f));
                SetImageColor(rightShoeImage, Darken(shoeColor, 0.68f));
                ConfigureDetail(leftShoeDetailImage, RoundedSprite(), Lighten(shoeColor, 0.4f), new Vector2(-22, -171), new Vector2(22, 3));
                ConfigureDetail(rightShoeDetailImage, RoundedSprite(), Lighten(shoeColor, 0.4f), new Vector2(22, -171), new Vector2(22, 3));
            }
            else if (value == CharacterAvatarOptions.Slippers)
            {
                size = new Vector2(38, 15);
                leftPosition = new Vector2(-22, -171);
                rightPosition = new Vector2(22, -171);
                leftShoeImage.sprite = CircleSprite();
                rightShoeImage.sprite = CircleSprite();
                leftShoeImage.rectTransform.localEulerAngles = new Vector3(0, 0, -6);
                rightShoeImage.rectTransform.localEulerAngles = new Vector3(0, 0, 6);
            }
            else
            {
                ConfigureDetail(leftShoeDetailImage, RoundedSprite(), Lighten(shoeColor, 0.48f), new Vector2(-22, -169), new Vector2(20, 4));
                ConfigureDetail(rightShoeDetailImage, RoundedSprite(), Lighten(shoeColor, 0.48f), new Vector2(22, -169), new Vector2(20, 4));
            }

            leftShoeImage.rectTransform.anchoredPosition = leftPosition;
            rightShoeImage.rectTransform.anchoredPosition = rightPosition;
            leftShoeImage.rectTransform.sizeDelta = size;
            rightShoeImage.rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// This function styles the selected eye shape with real image layers.
        /// </summary>
        private void ApplyEyesStyle(string eyes)
        {
            if (leftEyeImage == null || rightEyeImage == null || leftEyeWhiteImage == null || rightEyeWhiteImage == null)
            {
                return;
            }

            string value = CharacterAvatarId.NormalizeEyes(eyes);
            RectTransform leftWhiteRect = leftEyeWhiteImage.rectTransform;
            RectTransform rightWhiteRect = rightEyeWhiteImage.rectTransform;
            RectTransform leftRect = leftEyeImage.rectTransform;
            RectTransform rightRect = rightEyeImage.rectTransform;
            RectTransform leftBrowRect = leftBrowImage != null ? leftBrowImage.rectTransform : null;
            RectTransform rightBrowRect = rightBrowImage != null ? rightBrowImage.rectTransform : null;
            leftEyeWhiteImage.enabled = true;
            rightEyeWhiteImage.enabled = true;
            leftEyeImage.enabled = true;
            rightEyeImage.enabled = true;
            SetImageVisible(leftBrowImage, true);
            SetImageVisible(rightBrowImage, true);
            leftEyeWhiteImage.sprite = RoundedSprite();
            rightEyeWhiteImage.sprite = RoundedSprite();
            leftEyeImage.sprite = CircleSprite();
            rightEyeImage.sprite = CircleSprite();
            SetSprite(leftBrowImage, RoundedSprite());
            SetSprite(rightBrowImage, RoundedSprite());
            leftWhiteRect.localEulerAngles = Vector3.zero;
            rightWhiteRect.localEulerAngles = Vector3.zero;
            leftRect.localEulerAngles = Vector3.zero;
            rightRect.localEulerAngles = Vector3.zero;
            if (leftBrowRect != null)
            {
                leftBrowRect.localEulerAngles = new Vector3(0, 0, -4f);
                leftBrowRect.anchoredPosition = new Vector2(-22, 103);
                leftBrowRect.sizeDelta = new Vector2(23, 5);
            }

            if (rightBrowRect != null)
            {
                rightBrowRect.localEulerAngles = new Vector3(0, 0, 4f);
                rightBrowRect.anchoredPosition = new Vector2(22, 103);
                rightBrowRect.sizeDelta = new Vector2(23, 5);
            }

            leftWhiteRect.anchoredPosition = new Vector2(-22, 88);
            rightWhiteRect.anchoredPosition = new Vector2(22, 88);
            leftWhiteRect.sizeDelta = new Vector2(24, 13);
            rightWhiteRect.sizeDelta = new Vector2(24, 13);
            leftRect.anchoredPosition = new Vector2(-22, 88);
            rightRect.anchoredPosition = new Vector2(22, 88);
            leftRect.sizeDelta = new Vector2(8, 8);
            rightRect.sizeDelta = new Vector2(8, 8);

            if (value == CharacterAvatarOptions.Dot)
            {
                leftEyeWhiteImage.enabled = false;
                rightEyeWhiteImage.enabled = false;
                leftRect.sizeDelta = new Vector2(9, 9);
                rightRect.sizeDelta = new Vector2(9, 9);
            }
            else if (value == CharacterAvatarOptions.Happy || value == CharacterAvatarOptions.Sleepy)
            {
                leftEyeWhiteImage.enabled = false;
                rightEyeWhiteImage.enabled = false;
                leftEyeImage.sprite = RoundedSprite();
                rightEyeImage.sprite = RoundedSprite();
                leftRect.sizeDelta = new Vector2(value == CharacterAvatarOptions.Happy ? 20 : 22, 5);
                rightRect.sizeDelta = new Vector2(value == CharacterAvatarOptions.Happy ? 20 : 22, 5);
                leftRect.anchoredPosition = new Vector2(-22, value == CharacterAvatarOptions.Happy ? 91 : 87);
                rightRect.anchoredPosition = new Vector2(22, value == CharacterAvatarOptions.Happy ? 91 : 87);
                if (leftBrowRect != null)
                {
                    leftBrowRect.anchoredPosition = new Vector2(-22, value == CharacterAvatarOptions.Happy ? 106 : 96);
                    leftBrowRect.localEulerAngles = new Vector3(0, 0, value == CharacterAvatarOptions.Happy ? -10f : 0f);
                }

                if (rightBrowRect != null)
                {
                    rightBrowRect.anchoredPosition = new Vector2(22, value == CharacterAvatarOptions.Happy ? 106 : 96);
                    rightBrowRect.localEulerAngles = new Vector3(0, 0, value == CharacterAvatarOptions.Happy ? 10f : 0f);
                }
            }
            else if (value == CharacterAvatarOptions.Focused)
            {
                leftWhiteRect.localEulerAngles = new Vector3(0, 0, -10);
                rightWhiteRect.localEulerAngles = new Vector3(0, 0, 10);
                leftRect.sizeDelta = new Vector2(9, 9);
                rightRect.sizeDelta = new Vector2(9, 9);
                leftRect.localEulerAngles = new Vector3(0, 0, -12);
                rightRect.localEulerAngles = new Vector3(0, 0, 12);
                if (leftBrowRect != null)
                {
                    leftBrowRect.anchoredPosition = new Vector2(-22, 101);
                    leftBrowRect.localEulerAngles = new Vector3(0, 0, -16f);
                    leftBrowRect.sizeDelta = new Vector2(25, 5);
                }

                if (rightBrowRect != null)
                {
                    rightBrowRect.anchoredPosition = new Vector2(22, 101);
                    rightBrowRect.localEulerAngles = new Vector3(0, 0, 16f);
                    rightBrowRect.sizeDelta = new Vector2(25, 5);
                }
            }
        }

        /// <summary>
        /// This function styles the mouth as a small face layer instead of text.
        /// </summary>
        private void ApplyMouthStyle(string face, string species)
        {
            if (mouthImage == null)
            {
                return;
            }

            string value = CharacterAvatarId.NormalizeFace(face);
            if (CharacterAvatarId.NormalizeSpecies(species) == CharacterAvatarOptions.Elephant)
            {
                mouthImage.enabled = false;
                return;
            }

            RectTransform rect = mouthImage.rectTransform;
            mouthImage.enabled = true;
            mouthImage.sprite = RoundedSprite();
            rect.anchoredPosition = new Vector2(0, 58);
            rect.sizeDelta = new Vector2(30, 5);

            if (value == CharacterAvatarOptions.Sharp)
            {
                rect.sizeDelta = new Vector2(24, 5);
            }
            else if (value == CharacterAvatarOptions.LongFace)
            {
                rect.anchoredPosition = new Vector2(0, 54);
                rect.sizeDelta = new Vector2(28, 4);
            }
            else if (value == CharacterAvatarOptions.Snout)
            {
                rect.anchoredPosition = new Vector2(0, 48);
                rect.sizeDelta = new Vector2(24, 5);
            }
        }

        /// <summary>
        /// This function uses gender only for human proportions.
        /// </summary>
        private void ApplyGenderStyle(string sex, string species)
        {
            if (!IsHumanAvatarSpecies(CharacterAvatarId.NormalizeSpecies(species)) || bodyImage == null)
            {
                return;
            }

            RectTransform bodyRect = bodyImage.rectTransform;
            Vector2 bodySize = bodyRect.sizeDelta;

            if (CharacterAvatarId.NormalizeSex(sex) == CharacterAvatarOptions.Male)
            {
                bodyRect.sizeDelta = new Vector2(Mathf.Max(bodySize.x, 104), bodySize.y);
                WidenLayer(bottomImage, 5f);
                MoveLayerX(leftLegImage, -3f);
                MoveLayerX(rightLegImage, 3f);
                MoveLayerX(leftShoeImage, -3f);
                MoveLayerX(rightShoeImage, 3f);
                MoveLayerX(leftShoeDetailImage, -3f);
                MoveLayerX(rightShoeDetailImage, 3f);

                if (leftArmImage != null && rightArmImage != null)
                {
                    leftArmImage.rectTransform.anchoredPosition = new Vector2(-70, leftArmImage.rectTransform.anchoredPosition.y);
                    rightArmImage.rectTransform.anchoredPosition = new Vector2(70, rightArmImage.rectTransform.anchoredPosition.y);
                    MoveLayerX(leftHandImage, -3f);
                    MoveLayerX(rightHandImage, 3f);
                }
            }
            else
            {
                bodyRect.sizeDelta = new Vector2(Mathf.Min(bodySize.x, 90), bodySize.y);
                WidenLayer(bottomImage, 8f);
                MoveLayerX(leftArmImage, 4f);
                MoveLayerX(rightArmImage, -4f);
                MoveLayerX(leftHandImage, 4f);
                MoveLayerX(rightHandImage, -4f);
                MoveLayerX(leftLegImage, 2f);
                MoveLayerX(rightLegImage, -2f);
                MoveLayerX(leftShoeImage, 2f);
                MoveLayerX(rightShoeImage, -2f);
                MoveLayerX(leftShoeDetailImage, 2f);
                MoveLayerX(rightShoeDetailImage, -2f);
            }
        }

        /// <summary>
        /// This function positions the selected accessory image.
        /// </summary>
        private void ApplyAccessoryStyle(string accessory)
        {
            if (accessoryImage == null)
            {
                return;
            }

            RectTransform rect = accessoryImage.rectTransform;
            string value = CharacterAvatarId.NormalizeAccessory(accessory);
            HideAccessoryDetails();
            bool visible = value != CharacterAvatarOptions.NoAccessory
                && value != CharacterAvatarOptions.Wings
                && value != CharacterAvatarOptions.Tail
                && value != CharacterAvatarOptions.Horns;
            accessoryImage.enabled = visible;

            if (!visible)
            {
                return;
            }

            accessoryImage.sprite = RoundedSprite();
            accessoryImage.color = AccessoryColor(value);
            rect.localEulerAngles = Vector3.zero;
            rect.anchoredPosition = new Vector2(0, 90);
            rect.sizeDelta = new Vector2(86, 10);

            if (value == CharacterAvatarOptions.Glasses)
            {
                accessoryImage.color = new Color32(20, 30, 38, 255);
                rect.anchoredPosition = new Vector2(0, 88);
                rect.sizeDelta = new Vector2(68, 5);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(20, 30, 38, 215), new Vector2(-25, 88), new Vector2(28, 22));
                ConfigureDetail(accessoryRightDetailImage, RoundedSprite(), new Color32(20, 30, 38, 215), new Vector2(25, 88), new Vector2(28, 22));
            }
            else if (value == CharacterAvatarOptions.Cap)
            {
                rect.anchoredPosition = new Vector2(0, 130);
                rect.sizeDelta = new Vector2(94, 34);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), Darken(accessoryImage.color, 0.66f), new Vector2(0, 114), new Vector2(62, 12));
                ConfigureDetail(accessoryRightDetailImage, DiamondSprite(), Lighten(accessoryImage.color, 0.32f), new Vector2(0, 139), new Vector2(28, 18));
            }
            else if (value == CharacterAvatarOptions.Crown)
            {
                accessoryImage.sprite = DiamondSprite();
                rect.anchoredPosition = new Vector2(0, 150);
                rect.sizeDelta = new Vector2(52, 36);
                ConfigureDetail(accessoryLeftDetailImage, DiamondSprite(), new Color32(255, 235, 135, 255), new Vector2(-31, 140), new Vector2(30, 30));
                ConfigureDetail(accessoryRightDetailImage, DiamondSprite(), new Color32(255, 235, 135, 255), new Vector2(31, 140), new Vector2(30, 30));
            }
            else if (value == CharacterAvatarOptions.Mask)
            {
                rect.anchoredPosition = new Vector2(0, 66);
                rect.sizeDelta = new Vector2(64, 26);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(210, 232, 240, 255), new Vector2(-46, 67), new Vector2(22, 5), -8f);
                ConfigureDetail(accessoryRightDetailImage, RoundedSprite(), new Color32(210, 232, 240, 255), new Vector2(46, 67), new Vector2(22, 5), 8f);
                SetImageVisible(mouthImage, false);
            }
            else if (value == CharacterAvatarOptions.Headphones)
            {
                accessoryImage.color = new Color32(20, 30, 38, 255);
                rect.anchoredPosition = new Vector2(0, 94);
                rect.sizeDelta = new Vector2(116, 12);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(31, 50, 70, 255), new Vector2(-66, 78), new Vector2(24, 44));
                ConfigureDetail(accessoryRightDetailImage, RoundedSprite(), new Color32(31, 50, 70, 255), new Vector2(66, 78), new Vector2(24, 44));
            }
            else if (value == CharacterAvatarOptions.Halo)
            {
                rect.anchoredPosition = new Vector2(0, 158);
                rect.sizeDelta = new Vector2(88, 12);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(255, 246, 173, 180), new Vector2(0, 158), new Vector2(62, 5));
            }
        }

        /// <summary>
        /// This function resets shirt-only detail layers before applying another top style.
        /// </summary>
        private void HideTopDetails()
        {
            SetImageVisible(topCenterDetailImage, false);
            SetImageVisible(leftTopDetailImage, false);
            SetImageVisible(rightTopDetailImage, false);
        }

        /// <summary>
        /// This function resets pants, waist, and shoe detail layers before applying another clothing style.
        /// </summary>
        private void HideBottomDetails()
        {
            SetImageVisible(waistDetailImage, false);
            SetImageVisible(leftBottomDetailImage, false);
            SetImageVisible(rightBottomDetailImage, false);
        }

        private void HideHairDetails()
        {
            SetImageVisible(leftHairDetailImage, false);
            SetImageVisible(rightHairDetailImage, false);
        }

        private void HideAccessoryDetails()
        {
            SetImageVisible(accessoryLeftDetailImage, false);
            SetImageVisible(accessoryRightDetailImage, false);
        }

        private struct IllustratedAttachmentProfile
        {
            public Vector2 chestPoint;
            public Vector2 chestSize;
            public Vector2 leftSleevePoint;
            public Vector2 rightSleevePoint;
            public Vector2 sleeveSize;
            public float leftSleeveRotation;
            public float rightSleeveRotation;
            public Vector2 pantsPoint;
            public Vector2 pantsSize;
            public Vector2 leftLegPoint;
            public Vector2 rightLegPoint;
            public Vector2 legSize;
            public Vector2 leftShoePoint;
            public Vector2 rightShoePoint;
            public Vector2 shoeSize;
            public Vector2 facePoint;
            public Vector2 faceSize;
            public float faceRotation;
            public Vector2 leftStrapOffset;
            public Vector2 rightStrapOffset;
            public Vector2 strapSize;
            public Vector2 hatPoint;
            public Vector2 hatSize;
            public Vector2 crownPoint;
            public Vector2 crownSize;
            public Vector2 haloPoint;
            public Vector2 haloSize;
            public Vector2 wingsPoint;
            public Vector2 wingsSize;
            public Vector2 tailPoint;
            public Vector2 tailSize;
            public float tailRotation;
            public Vector2 leftHornPoint;
            public Vector2 rightHornPoint;
            public Vector2 hornSize;

            // These attachment profiles replace the old one-size-fits-all overlay.
            // Each coordinate is local to the avatar preview, so the same clothes attach to species-specific body points.
            public static IllustratedAttachmentProfile ForSpecies(string species)
            {
                IllustratedAttachmentProfile profile = Human();

                if (species == CharacterAvatarOptions.Dragon)
                {
                    profile.chestPoint = new Vector2(-5, -32);
                    profile.chestSize = new Vector2(74, 62);
                    profile.leftSleevePoint = new Vector2(-61, -48);
                    profile.rightSleevePoint = new Vector2(50, -47);
                    profile.sleeveSize = new Vector2(20, 54);
                    profile.leftSleeveRotation = 12f;
                    profile.rightSleeveRotation = -8f;
                    profile.pantsPoint = new Vector2(-6, -75);
                    profile.pantsSize = new Vector2(70, 24);
                    profile.leftLegPoint = new Vector2(-45, -100);
                    profile.rightLegPoint = new Vector2(34, -100);
                    profile.legSize = new Vector2(18, 42);
                    profile.leftShoePoint = new Vector2(-54, -123);
                    profile.rightShoePoint = new Vector2(43, -123);
                    profile.shoeSize = new Vector2(28, 13);
                    profile.facePoint = new Vector2(-41, 42);
                    profile.faceSize = new Vector2(72, 22);
                    profile.faceRotation = -12f;
                    profile.leftStrapOffset = new Vector2(-44, -2);
                    profile.rightStrapOffset = new Vector2(44, 8);
                    profile.strapSize = new Vector2(23, 5);
                    profile.hatPoint = new Vector2(-24, 108);
                    profile.hatSize = new Vector2(78, 26);
                    profile.crownPoint = new Vector2(-18, 132);
                    profile.haloPoint = new Vector2(-18, 142);
                    profile.wingsPoint = new Vector2(0, -18);
                    profile.wingsSize = new Vector2(250, 122);
                    profile.tailPoint = new Vector2(76, -100);
                    profile.tailSize = new Vector2(82, 12);
                    profile.tailRotation = -15f;
                    profile.leftHornPoint = new Vector2(-32, 124);
                    profile.rightHornPoint = new Vector2(18, 129);
                }
                else if (species == CharacterAvatarOptions.Bear)
                {
                    profile.chestPoint = new Vector2(0, -34);
                    profile.chestSize = new Vector2(106, 76);
                    profile.leftSleevePoint = new Vector2(-70, -42);
                    profile.rightSleevePoint = new Vector2(70, -42);
                    profile.sleeveSize = new Vector2(24, 66);
                    profile.pantsPoint = new Vector2(0, -84);
                    profile.pantsSize = new Vector2(96, 28);
                    profile.leftLegPoint = new Vector2(-31, -115);
                    profile.rightLegPoint = new Vector2(31, -115);
                    profile.legSize = new Vector2(24, 42);
                    profile.leftShoePoint = new Vector2(-34, -139);
                    profile.rightShoePoint = new Vector2(34, -139);
                    profile.shoeSize = new Vector2(34, 15);
                    profile.facePoint = new Vector2(0, 55);
                    profile.faceSize = new Vector2(76, 24);
                    profile.hatPoint = new Vector2(0, 118);
                    profile.crownPoint = new Vector2(0, 145);
                    profile.haloPoint = new Vector2(0, 158);
                    profile.leftHornPoint = new Vector2(-37, 126);
                    profile.rightHornPoint = new Vector2(37, 126);
                }
                else if (species == CharacterAvatarOptions.Elephant)
                {
                    profile.chestPoint = new Vector2(0, -38);
                    profile.chestSize = new Vector2(116, 86);
                    profile.leftSleevePoint = new Vector2(-73, -42);
                    profile.rightSleevePoint = new Vector2(73, -42);
                    profile.sleeveSize = new Vector2(26, 72);
                    profile.pantsPoint = new Vector2(0, -88);
                    profile.pantsSize = new Vector2(100, 30);
                    profile.leftLegPoint = new Vector2(-34, -118);
                    profile.rightLegPoint = new Vector2(34, -118);
                    profile.legSize = new Vector2(24, 48);
                    profile.leftShoePoint = new Vector2(-35, -145);
                    profile.rightShoePoint = new Vector2(35, -145);
                    profile.shoeSize = new Vector2(34, 16);
                    profile.facePoint = new Vector2(0, 31);
                    profile.faceSize = new Vector2(68, 23);
                    profile.hatPoint = new Vector2(0, 122);
                    profile.hatSize = new Vector2(96, 28);
                    profile.crownPoint = new Vector2(0, 148);
                    profile.haloPoint = new Vector2(0, 158);
                    profile.leftHornPoint = new Vector2(-42, 126);
                    profile.rightHornPoint = new Vector2(42, 126);
                }
                else if (species == CharacterAvatarOptions.Devil)
                {
                    profile.chestPoint = new Vector2(0, -36);
                    profile.chestSize = new Vector2(86, 68);
                    profile.leftSleevePoint = new Vector2(-60, -43);
                    profile.rightSleevePoint = new Vector2(60, -43);
                    profile.pantsPoint = new Vector2(0, -84);
                    profile.leftLegPoint = new Vector2(-23, -123);
                    profile.rightLegPoint = new Vector2(23, -123);
                    profile.leftShoePoint = new Vector2(-23, -154);
                    profile.rightShoePoint = new Vector2(23, -154);
                    profile.facePoint = new Vector2(0, 54);
                    profile.faceSize = new Vector2(66, 22);
                    profile.hatPoint = new Vector2(0, 116);
                    profile.crownPoint = new Vector2(0, 142);
                    profile.haloPoint = new Vector2(0, 152);
                    profile.tailPoint = new Vector2(73, -82);
                    profile.tailRotation = 16f;
                    profile.leftHornPoint = new Vector2(-38, 135);
                    profile.rightHornPoint = new Vector2(38, 135);
                }
                else if (species == CharacterAvatarOptions.Angel)
                {
                    profile.chestPoint = new Vector2(0, -34);
                    profile.chestSize = new Vector2(92, 88);
                    profile.leftSleevePoint = new Vector2(-56, -38);
                    profile.rightSleevePoint = new Vector2(56, -38);
                    profile.pantsPoint = new Vector2(0, -91);
                    profile.pantsSize = new Vector2(96, 40);
                    profile.leftLegPoint = new Vector2(-21, -136);
                    profile.rightLegPoint = new Vector2(21, -136);
                    profile.leftShoePoint = new Vector2(-21, -158);
                    profile.rightShoePoint = new Vector2(21, -158);
                    profile.facePoint = new Vector2(0, 60);
                    profile.faceSize = new Vector2(64, 22);
                    profile.hatPoint = new Vector2(0, 126);
                    profile.crownPoint = new Vector2(0, 148);
                    profile.haloPoint = new Vector2(0, 164);
                    profile.wingsPoint = new Vector2(0, 4);
                    profile.wingsSize = new Vector2(260, 150);
                }

                return profile;
            }

            private static IllustratedAttachmentProfile Human()
            {
                return new IllustratedAttachmentProfile
                {
                    chestPoint = new Vector2(0, -44),
                    chestSize = new Vector2(86, 68),
                    leftSleevePoint = new Vector2(-56, -44),
                    rightSleevePoint = new Vector2(56, -44),
                    sleeveSize = new Vector2(18, 68),
                    leftSleeveRotation = -10f,
                    rightSleeveRotation = 10f,
                    pantsPoint = new Vector2(0, -88),
                    pantsSize = new Vector2(76, 30),
                    leftLegPoint = new Vector2(-19, -126),
                    rightLegPoint = new Vector2(19, -126),
                    legSize = new Vector2(18, 66),
                    leftShoePoint = new Vector2(-19, -163),
                    rightShoePoint = new Vector2(19, -163),
                    shoeSize = new Vector2(32, 15),
                    facePoint = new Vector2(0, 58),
                    faceSize = new Vector2(62, 24),
                    faceRotation = 0f,
                    leftStrapOffset = new Vector2(-45, 1),
                    rightStrapOffset = new Vector2(45, 1),
                    strapSize = new Vector2(22, 5),
                    hatPoint = new Vector2(0, 126),
                    hatSize = new Vector2(92, 30),
                    crownPoint = new Vector2(0, 145),
                    crownSize = new Vector2(56, 34),
                    haloPoint = new Vector2(0, 156),
                    haloSize = new Vector2(88, 12),
                    wingsPoint = new Vector2(0, -12),
                    wingsSize = new Vector2(260, 132),
                    tailPoint = new Vector2(72, -118),
                    tailSize = new Vector2(72, 12),
                    tailRotation = -18f,
                    leftHornPoint = new Vector2(-36, 132),
                    rightHornPoint = new Vector2(36, 132),
                    hornSize = new Vector2(22, 34)
                };
            }
        }

        /// <summary>
        /// This function loads the generated species art and hides the procedural avatar layers.
        /// </summary>
        private bool ApplyIllustratedAvatar(string species)
        {
            Sprite sprite = LoadArtSprite(CharacterAvatarId.NormalizeSpecies(species));
            bool hasArt = sprite != null;
            SetImageVisible(artImage, hasArt);

            if (!hasArt)
            {
                return false;
            }

            SetProceduralLayersVisible(false);
            if (badgeLabel != null)
            {
                badgeLabel.enabled = false;
            }

            artImage.sprite = sprite;
            artImage.color = Color.white;
            artImage.preserveAspect = true;
            artImage.rectTransform.anchoredPosition = new Vector2(0, -8);
            artImage.rectTransform.sizeDelta = new Vector2(286, 326);
            SetImageColor(backgroundImage, new Color32(10, 21, 36, 255));
            return true;
        }

        /// <summary>
        /// This function draws the saved clothing and accessories over the illustrated base character.
        /// </summary>
        private void ApplyIllustratedOverlays(CharacterAvatarSpec spec, Color topColor, Color bottomColor, Color shoeColor)
        {
            HideTopDetails();
            HideBottomDetails();
            HideAccessoryDetails();
            HideIllustratedOverlayLayers();

            string species = CharacterAvatarId.NormalizeSpecies(spec.species);
            string top = CharacterAvatarId.NormalizeTop(spec.top);
            string bottom = CharacterAvatarId.NormalizeBottom(spec.bottom);
            string shoes = CharacterAvatarId.NormalizeShoes(spec.shoes);
            string accessory = CharacterAvatarId.NormalizeAccessory(spec.accessory);

            IllustratedAttachmentProfile profile = IllustratedAttachmentProfile.ForSpecies(species);

            bodyImage.enabled = true;
            bodyImage.sprite = top == CharacterAvatarOptions.Dress ? SkirtSprite() : TorsoSprite();
            bodyImage.color = OverlayColor(topColor, 210);
            bodyImage.rectTransform.anchoredPosition = profile.chestPoint;
            bodyImage.rectTransform.sizeDelta = top == CharacterAvatarOptions.Dress
                ? new Vector2(profile.chestSize.x + 26, profile.chestSize.y + 54)
                : profile.chestSize;
            bodyImage.rectTransform.localEulerAngles = Vector3.zero;

            if (badgeLabel != null)
            {
                badgeLabel.enabled = true;
                badgeLabel.rectTransform.anchoredPosition = profile.chestPoint + new Vector2(0, -6);
                badgeLabel.rectTransform.sizeDelta = new Vector2(28, 20);
            }

            bool sleevesVisible = top != CharacterAvatarOptions.Vest && top != CharacterAvatarOptions.Armor;
            PositionOverlay(leftArmImage, sleevesVisible, profile.leftSleevePoint, profile.sleeveSize, profile.leftSleeveRotation, OverlayColor(topColor, 205), RoundedSprite());
            PositionOverlay(rightArmImage, sleevesVisible, profile.rightSleevePoint, profile.sleeveSize, profile.rightSleeveRotation, OverlayColor(topColor, 205), RoundedSprite());

            ApplyIllustratedTopDetails(top, topColor, profile.chestPoint, profile.chestSize);
            ApplyIllustratedBottom(bottom, top, bottomColor, profile.leftLegPoint, profile.rightLegPoint, profile.legSize, profile.pantsPoint, profile.pantsSize);
            ApplyIllustratedShoes(shoes, shoeColor, profile.leftShoePoint, profile.rightShoePoint, profile.shoeSize);
            ApplyIllustratedAccessory(accessory, profile);
        }

        /// <summary>
        /// This function adds shirt-specific details, such as hood pockets, collars, jackets, and armor plates.
        /// </summary>
        private void ApplyIllustratedTopDetails(string top, Color topColor, Vector2 bodyPosition, Vector2 bodySize)
        {
            if (top == CharacterAvatarOptions.Hoodie)
            {
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), OverlayColor(Darken(topColor, 0.72f), 235), bodyPosition + new Vector2(0, -25), new Vector2(bodySize.x * 0.62f, 18));
                ConfigureDetail(leftTopDetailImage, RoundedSprite(), OverlayColor(Lighten(topColor, 0.55f), 235), bodyPosition + new Vector2(-10, 26), new Vector2(4, 34), -7f);
                ConfigureDetail(rightTopDetailImage, RoundedSprite(), OverlayColor(Lighten(topColor, 0.55f), 235), bodyPosition + new Vector2(10, 26), new Vector2(4, 34), 7f);
            }
            else if (top == CharacterAvatarOptions.Shirt)
            {
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), OverlayColor(Darken(topColor, 0.55f), 230), bodyPosition + new Vector2(0, -5), new Vector2(5, bodySize.y * 0.78f));
                ConfigureDetail(leftTopDetailImage, DiamondSprite(), OverlayColor(Lighten(topColor, 0.62f), 225), bodyPosition + new Vector2(-17, 28), new Vector2(30, 24), -8f);
                ConfigureDetail(rightTopDetailImage, DiamondSprite(), OverlayColor(Lighten(topColor, 0.62f), 225), bodyPosition + new Vector2(17, 28), new Vector2(30, 24), 8f);
            }
            else if (top == CharacterAvatarOptions.Jacket || top == CharacterAvatarOptions.Vest)
            {
                ConfigureDetail(topCenterDetailImage, RoundedSprite(), OverlayColor(Lighten(topColor, 0.6f), 235), bodyPosition + new Vector2(0, -5), new Vector2(5, bodySize.y * 0.82f));
                ConfigureDetail(leftTopDetailImage, DiamondSprite(), OverlayColor(Darken(topColor, 0.62f), 220), bodyPosition + new Vector2(-23, -7), new Vector2(38, 62), -4f);
                ConfigureDetail(rightTopDetailImage, DiamondSprite(), OverlayColor(Darken(topColor, 0.62f), 220), bodyPosition + new Vector2(23, -7), new Vector2(38, 62), 4f);
            }
            else if (top == CharacterAvatarOptions.Armor)
            {
                bodyImage.sprite = DiamondSprite();
                ConfigureDetail(topCenterDetailImage, DiamondSprite(), OverlayColor(new Color32(188, 198, 211, 255), 230), bodyPosition + new Vector2(0, -3), new Vector2(58, 58));
                ConfigureDetail(leftTopDetailImage, DiamondSprite(), OverlayColor(new Color32(118, 126, 140, 255), 235), bodyPosition + new Vector2(-46, 8), new Vector2(30, 30), -8f);
                ConfigureDetail(rightTopDetailImage, DiamondSprite(), OverlayColor(new Color32(118, 126, 140, 255), 235), bodyPosition + new Vector2(46, 8), new Vector2(30, 30), 8f);
            }
            else
            {
                ConfigureDetail(topCenterDetailImage, DiamondSprite(), OverlayColor(Lighten(topColor, 0.38f), 220), bodyPosition + new Vector2(0, 28), new Vector2(32, 18));
            }
        }

        /// <summary>
        /// This function adds pants, skirts, waist details, cargo pockets, and side stripes over the art.
        /// </summary>
        private void ApplyIllustratedBottom(
            string bottom,
            string top,
            Color bottomColor,
            Vector2 leftLegPosition,
            Vector2 rightLegPosition,
            Vector2 legSize,
            Vector2 bottomPosition,
            Vector2 bottomSize)
        {
            bool dress = top == CharacterAvatarOptions.Dress;
            bottomImage.enabled = true;
            bottomImage.color = OverlayColor(bottomColor, 220);
            bottomImage.sprite = dress || bottom == CharacterAvatarOptions.Skirt ? SkirtSprite() : RoundedSprite();
            bottomImage.rectTransform.anchoredPosition = dress ? bottomPosition + new Vector2(0, -12) : bottomPosition;
            bottomImage.rectTransform.sizeDelta = dress ? new Vector2(bottomSize.x + 34, 72) : bottomSize;
            bottomImage.rectTransform.localEulerAngles = Vector3.zero;

            bool showLegs = bottom != CharacterAvatarOptions.Skirt && !dress;
            PositionOverlay(leftLegImage, showLegs, leftLegPosition, legSize, 0f, OverlayColor(bottomColor, 220), LegSprite());
            PositionOverlay(rightLegImage, showLegs, rightLegPosition, legSize, 0f, OverlayColor(bottomColor, 220), LegSprite());
            ConfigureDetail(waistDetailImage, RoundedSprite(), OverlayColor(Darken(bottomColor, 0.65f), 235), bottomPosition + new Vector2(0, 13), new Vector2(bottomSize.x + 4, 8));

            if (bottom == CharacterAvatarOptions.Cargo)
            {
                ConfigureDetail(leftBottomDetailImage, RoundedSprite(), OverlayColor(Darken(bottomColor, 0.72f), 235), leftLegPosition + new Vector2(0, 0), new Vector2(18, 20));
                ConfigureDetail(rightBottomDetailImage, RoundedSprite(), OverlayColor(Darken(bottomColor, 0.72f), 235), rightLegPosition + new Vector2(0, 0), new Vector2(18, 20));
            }
            else if (bottom == CharacterAvatarOptions.Training || bottom == CharacterAvatarOptions.Leggings)
            {
                ConfigureDetail(leftBottomDetailImage, RoundedSprite(), OverlayColor(Lighten(bottomColor, 0.46f), 235), leftLegPosition + new Vector2(-13, 0), new Vector2(4, legSize.y));
                ConfigureDetail(rightBottomDetailImage, RoundedSprite(), OverlayColor(Lighten(bottomColor, 0.46f), 235), rightLegPosition + new Vector2(13, 0), new Vector2(4, legSize.y));
            }
        }

        /// <summary>
        /// This function places the selected shoes and their highlights over the illustrated feet.
        /// </summary>
        private void ApplyIllustratedShoes(string shoes, Color shoeColor, Vector2 leftPosition, Vector2 rightPosition, Vector2 baseSize)
        {
            bool visible = shoes != CharacterAvatarOptions.NoShoes;
            Vector2 size = shoes == CharacterAvatarOptions.Boots ? new Vector2(baseSize.x, baseSize.y + 15) : baseSize;
            float yOffset = shoes == CharacterAvatarOptions.Boots ? baseSize.y * 0.45f : 0f;
            Sprite sprite = shoes == CharacterAvatarOptions.Slippers ? CircleSprite() : ShoeSprite();
            Color color = shoes == CharacterAvatarOptions.Boots ? OverlayColor(Darken(shoeColor, 0.78f), 235) : OverlayColor(shoeColor, 235);
            PositionOverlay(leftShoeImage, visible, leftPosition + new Vector2(0, yOffset), size, shoes == CharacterAvatarOptions.Slippers ? -6f : 0f, color, sprite);
            PositionOverlay(rightShoeImage, visible, rightPosition + new Vector2(0, yOffset), size, shoes == CharacterAvatarOptions.Slippers ? 6f : 0f, color, sprite);

            if (!visible)
            {
                return;
            }

            ConfigureDetail(leftShoeDetailImage, RoundedSprite(), OverlayColor(Lighten(shoeColor, 0.52f), 240), leftPosition + new Vector2(0, 2 + yOffset), new Vector2(20, 4));
            ConfigureDetail(rightShoeDetailImage, RoundedSprite(), OverlayColor(Lighten(shoeColor, 0.52f), 240), rightPosition + new Vector2(0, 2 + yOffset), new Vector2(20, 4));
        }

        /// <summary>
        /// This function draws the chosen accessory as simple layered geometry above the species art.
        /// </summary>
        private void ApplyIllustratedAccessory(string accessory, IllustratedAttachmentProfile profile)
        {
            if (accessory == CharacterAvatarOptions.NoAccessory)
            {
                return;
            }

            if (accessory == CharacterAvatarOptions.Glasses)
            {
                ConfigureDetail(accessoryImage, RoundedSprite(), new Color32(9, 16, 23, 245), profile.facePoint + new Vector2(0, 18), new Vector2(profile.faceSize.x, 5), profile.faceRotation);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(9, 16, 23, 220), profile.facePoint + new Vector2(-profile.faceSize.x * 0.28f, 18), new Vector2(profile.faceSize.x * 0.38f, 20), profile.faceRotation);
                ConfigureDetail(accessoryRightDetailImage, RoundedSprite(), new Color32(9, 16, 23, 220), profile.facePoint + new Vector2(profile.faceSize.x * 0.28f, 18), new Vector2(profile.faceSize.x * 0.38f, 20), profile.faceRotation);
            }
            else if (accessory == CharacterAvatarOptions.Cap)
            {
                ConfigureDetail(accessoryImage, RoundedSprite(), new Color32(36, 79, 158, 245), profile.hatPoint, profile.hatSize);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(24, 52, 104, 245), profile.hatPoint + new Vector2(0, -profile.hatSize.y * 0.45f), new Vector2(profile.hatSize.x * 0.7f, 11));
            }
            else if (accessory == CharacterAvatarOptions.Crown)
            {
                ConfigureDetail(accessoryImage, DiamondSprite(), new Color32(255, 211, 106, 245), profile.crownPoint, profile.crownSize);
                ConfigureDetail(accessoryLeftDetailImage, DiamondSprite(), new Color32(255, 235, 135, 235), profile.crownPoint + new Vector2(-profile.crownSize.x * 0.5f, -8), new Vector2(26, 26));
                ConfigureDetail(accessoryRightDetailImage, DiamondSprite(), new Color32(255, 235, 135, 235), profile.crownPoint + new Vector2(profile.crownSize.x * 0.5f, -8), new Vector2(26, 26));
            }
            else if (accessory == CharacterAvatarOptions.Mask)
            {
                ConfigureDetail(accessoryImage, RoundedSprite(), new Color32(232, 247, 255, 245), profile.facePoint, profile.faceSize, profile.faceRotation);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(210, 232, 240, 245), profile.facePoint + profile.leftStrapOffset, profile.strapSize, profile.faceRotation - 8f);
                ConfigureDetail(accessoryRightDetailImage, RoundedSprite(), new Color32(210, 232, 240, 245), profile.facePoint + profile.rightStrapOffset, profile.strapSize, profile.faceRotation + 8f);
            }
            else if (accessory == CharacterAvatarOptions.Headphones)
            {
                ConfigureDetail(accessoryImage, RoundedSprite(), new Color32(10, 17, 26, 245), profile.facePoint + new Vector2(0, 22), new Vector2(profile.faceSize.x + 48, 12));
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(31, 50, 70, 245), profile.facePoint + new Vector2(-profile.faceSize.x * 0.75f, 6), new Vector2(22, 42));
                ConfigureDetail(accessoryRightDetailImage, RoundedSprite(), new Color32(31, 50, 70, 245), profile.facePoint + new Vector2(profile.faceSize.x * 0.75f, 6), new Vector2(22, 42));
            }
            else if (accessory == CharacterAvatarOptions.Halo)
            {
                ConfigureDetail(accessoryImage, RoundedSprite(), new Color32(255, 211, 106, 230), profile.haloPoint, profile.haloSize);
                ConfigureDetail(accessoryLeftDetailImage, RoundedSprite(), new Color32(255, 246, 173, 190), profile.haloPoint, new Vector2(profile.haloSize.x * 0.7f, 5));
            }
            else if (accessory == CharacterAvatarOptions.Wings)
            {
                ConfigureDetail(wingsImage, RoundedSprite(), new Color32(248, 250, 252, 190), profile.wingsPoint, profile.wingsSize);
            }
            else if (accessory == CharacterAvatarOptions.Tail)
            {
                ConfigureDetail(tailImage, RoundedSprite(), new Color32(178, 55, 55, 230), profile.tailPoint, profile.tailSize, profile.tailRotation);
            }
            else if (accessory == CharacterAvatarOptions.Horns)
            {
                ConfigureDetail(accessoryLeftDetailImage, DiamondSprite(), new Color32(178, 55, 55, 245), profile.leftHornPoint, profile.hornSize, -6f);
                ConfigureDetail(accessoryRightDetailImage, DiamondSprite(), new Color32(178, 55, 55, 245), profile.rightHornPoint, profile.hornSize, 6f);
            }
        }

        /// <summary>
        /// This function clears overlay-only layers before drawing a different saved outfit.
        /// </summary>
        private void HideIllustratedOverlayLayers()
        {
            SetImageVisible(wingsImage, false);
            SetImageVisible(tailImage, false);
            SetImageVisible(leftArmImage, false);
            SetImageVisible(rightArmImage, false);
            SetImageVisible(leftHandImage, false);
            SetImageVisible(rightHandImage, false);
            SetImageVisible(bodyImage, false);
            SetImageVisible(bottomImage, false);
            SetImageVisible(leftLegImage, false);
            SetImageVisible(rightLegImage, false);
            SetImageVisible(leftShoeImage, false);
            SetImageVisible(rightShoeImage, false);
            SetImageVisible(leftShoeDetailImage, false);
            SetImageVisible(rightShoeDetailImage, false);
            SetImageVisible(accessoryImage, false);
        }

        /// <summary>
        /// This function applies the common transform, sprite, and color settings for overlay pieces.
        /// </summary>
        private void PositionOverlay(Image image, bool visible, Vector2 position, Vector2 size, float zRotation, Color color, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = visible;
            if (!visible)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localEulerAngles = new Vector3(0, 0, zRotation);
        }

        private void SetProceduralLayersVisible(bool visible)
        {
            SetImageVisible(wingsImage, visible);
            SetImageVisible(tailImage, visible);
            SetImageVisible(leftArmImage, visible);
            SetImageVisible(rightArmImage, visible);
            SetImageVisible(leftHandImage, visible);
            SetImageVisible(rightHandImage, visible);
            SetImageVisible(bodyImage, visible);
            SetImageVisible(topCenterDetailImage, visible);
            SetImageVisible(leftTopDetailImage, visible);
            SetImageVisible(rightTopDetailImage, visible);
            SetImageVisible(neckImage, visible);
            SetImageVisible(bottomImage, visible);
            SetImageVisible(waistDetailImage, visible);
            SetImageVisible(leftBottomDetailImage, visible);
            SetImageVisible(rightBottomDetailImage, visible);
            SetImageVisible(leftLegImage, visible);
            SetImageVisible(rightLegImage, visible);
            SetImageVisible(leftShoeImage, visible);
            SetImageVisible(rightShoeImage, visible);
            SetImageVisible(leftShoeDetailImage, visible);
            SetImageVisible(rightShoeDetailImage, visible);
            SetImageVisible(leftEarImage, visible);
            SetImageVisible(rightEarImage, visible);
            SetImageVisible(headImage, visible);
            SetImageVisible(backHairImage, visible);
            SetImageVisible(hairImage, visible);
            SetImageVisible(leftHairDetailImage, visible);
            SetImageVisible(rightHairDetailImage, visible);
            SetImageVisible(jawShadowImage, visible);
            SetImageVisible(leftCheekImage, visible);
            SetImageVisible(rightCheekImage, visible);
            SetImageVisible(trunkImage, visible);
            SetImageVisible(leftEyeWhiteImage, visible);
            SetImageVisible(rightEyeWhiteImage, visible);
            SetImageVisible(leftEyeImage, visible);
            SetImageVisible(rightEyeImage, visible);
            SetImageVisible(leftBrowImage, visible);
            SetImageVisible(rightBrowImage, visible);
            SetImageVisible(mouthImage, visible);
            SetImageVisible(accessoryImage, visible);
            SetImageVisible(accessoryLeftDetailImage, visible);
            SetImageVisible(accessoryRightDetailImage, visible);

            if (badgeLabel != null)
            {
                badgeLabel.enabled = visible;
            }
        }

        /// <summary>
        /// This function adds small shadows and cheek tones so the face reads as a character, not a flat block.
        /// </summary>
        private void ApplyFaceDetailLayers(string face, string species, Color skinColor)
        {
            bool humanLike = IsHumanAvatarSpecies(species)
                || species == CharacterAvatarOptions.Angel
                || species == CharacterAvatarOptions.Devil;
            SetImageVisible(jawShadowImage, humanLike);
            SetImageVisible(leftCheekImage, humanLike);
            SetImageVisible(rightCheekImage, humanLike);

            if (!humanLike)
            {
                return;
            }

            ConfigureDetail(jawShadowImage, RoundedSprite(), Darken(skinColor, 0.86f), new Vector2(0, 42), new Vector2(42, 8));
            ConfigureDetail(leftCheekImage, CircleSprite(), CheekColorForSkin(skinColor), new Vector2(-34, 70), new Vector2(16, 10));
            ConfigureDetail(rightCheekImage, CircleSprite(), CheekColorForSkin(skinColor), new Vector2(34, 70), new Vector2(16, 10));

            if (face == CharacterAvatarOptions.LongFace)
            {
                jawShadowImage.rectTransform.anchoredPosition = new Vector2(0, 35);
                leftCheekImage.rectTransform.anchoredPosition = new Vector2(-31, 68);
                rightCheekImage.rectTransform.anchoredPosition = new Vector2(31, 68);
            }
            else if (face == CharacterAvatarOptions.Sharp)
            {
                jawShadowImage.rectTransform.sizeDelta = new Vector2(34, 7);
                jawShadowImage.rectTransform.anchoredPosition = new Vector2(0, 36);
            }
        }

        /// <summary>
        /// This function makes a small reusable clothing detail visible and positions it.
        /// </summary>
        private void ConfigureDetail(Image image, Sprite sprite, Color color, Vector2 position, Vector2 size, float zRotation = 0f)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = true;
            image.sprite = sprite;
            image.color = color;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localEulerAngles = new Vector3(0, 0, zRotation);
        }

        private void MoveLayerX(Image image, float offset)
        {
            if (image == null || !image.enabled)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + offset, rect.anchoredPosition.y);
        }

        private void WidenLayer(Image image, float amount)
        {
            if (image == null || !image.enabled)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x + amount, rect.sizeDelta.y);
        }

        /// <summary>
        /// This function maps one shared avatar color id to a Unity color.
        /// </summary>
        public static Color ColorForAvatar(string color)
        {
            switch (CharacterAvatarId.NormalizeColor(color))
            {
                case AvatarIds.Mint:
                    return new Color32(115, 226, 167, 255);
                case AvatarIds.Sun:
                    return new Color32(255, 211, 106, 255);
                case AvatarIds.Rose:
                    return new Color32(255, 155, 176, 255);
                case AvatarIds.Violet:
                    return new Color32(196, 167, 255, 255);
                case AvatarIds.Steel:
                    return new Color32(184, 199, 217, 255);
                case CharacterAvatarOptions.Coral:
                    return new Color32(255, 132, 115, 255);
                case CharacterAvatarOptions.Lime:
                    return new Color32(174, 230, 111, 255);
                case CharacterAvatarOptions.Sky:
                    return new Color32(120, 198, 255, 255);
                case CharacterAvatarOptions.Peach:
                    return new Color32(255, 187, 143, 255);
                case CharacterAvatarOptions.Navy:
                    return new Color32(32, 58, 99, 255);
                case CharacterAvatarOptions.White:
                    return new Color32(248, 250, 252, 255);
                case CharacterAvatarOptions.Black:
                    return new Color32(34, 40, 49, 255);
                case CharacterAvatarOptions.Red:
                    return new Color32(224, 79, 95, 255);
                case CharacterAvatarOptions.Green:
                    return new Color32(47, 158, 68, 255);
                case CharacterAvatarOptions.Denim:
                    return new Color32(65, 105, 168, 255);
                default:
                    return new Color32(102, 217, 239, 255);
            }
        }

        /// <summary>
        /// This function maps one skin tone id to a Unity color.
        /// </summary>
        public static Color SkinColor(string skin)
        {
            switch (CharacterAvatarId.NormalizeSkin(skin))
            {
                case CharacterAvatarOptions.Porcelain:
                    return new Color32(255, 230, 213, 255);
                case CharacterAvatarOptions.Light:
                    return new Color32(255, 214, 185, 255);
                case CharacterAvatarOptions.Brown:
                    return new Color32(167, 105, 71, 255);
                case CharacterAvatarOptions.Deep:
                    return new Color32(105, 63, 45, 255);
                case CharacterAvatarOptions.Green:
                    return new Color32(89, 176, 111, 255);
                case CharacterAvatarOptions.Red:
                    return new Color32(201, 79, 69, 255);
                case CharacterAvatarOptions.Gray:
                    return new Color32(154, 164, 173, 255);
                case CharacterAvatarOptions.Gold:
                    return new Color32(216, 170, 69, 255);
                default:
                    return new Color32(218, 154, 105, 255);
            }
        }

        /// <summary>
        /// This function maps one hair color id to a Unity color.
        /// </summary>
        public static Color HairColor(string color)
        {
            switch (CharacterAvatarId.NormalizeHairColor(color))
            {
                case CharacterAvatarOptions.Black:
                    return new Color32(35, 31, 32, 255);
                case CharacterAvatarOptions.Blonde:
                    return new Color32(234, 193, 93, 255);
                case CharacterAvatarOptions.HairRed:
                    return new Color32(172, 72, 49, 255);
                case CharacterAvatarOptions.HairBlue:
                    return new Color32(54, 106, 184, 255);
                case CharacterAvatarOptions.Pink:
                    return new Color32(217, 111, 177, 255);
                case CharacterAvatarOptions.Silver:
                    return new Color32(194, 197, 203, 255);
                case CharacterAvatarOptions.White:
                    return new Color32(243, 244, 246, 255);
                default:
                    return new Color32(91, 55, 36, 255);
            }
        }

        /// <summary>
        /// This function maps one eye color id to a Unity color.
        /// </summary>
        public static Color EyeColor(string color)
        {
            switch (CharacterAvatarId.NormalizeEyeColor(color))
            {
                case CharacterAvatarOptions.EyeBlue:
                    return new Color32(47, 128, 237, 255);
                case CharacterAvatarOptions.EyeGreen:
                    return new Color32(47, 158, 68, 255);
                case CharacterAvatarOptions.Hazel:
                    return new Color32(138, 111, 42, 255);
                case CharacterAvatarOptions.EyeViolet:
                    return new Color32(124, 92, 255, 255);
                case CharacterAvatarOptions.Amber:
                    return new Color32(212, 139, 40, 255);
                case CharacterAvatarOptions.EyeGray:
                    return new Color32(123, 135, 148, 255);
                default:
                    return new Color32(91, 55, 36, 255);
            }
        }

        /// <summary>
        /// This function maps one avatar color id to a readable text color.
        /// </summary>
        public static Color ContrastColor(string baseColor)
        {
            string value = CharacterAvatarId.NormalizeColor(baseColor);

            if (value == AvatarIds.Sun
                || value == CharacterAvatarOptions.Peach
                || value == CharacterAvatarOptions.Lime
                || value == CharacterAvatarOptions.White)
            {
                return new Color32(40, 40, 40, 255);
            }

            return Color.white;
        }

        private static Color ColorForFaceDetail(string skin)
        {
            string value = CharacterAvatarId.NormalizeSkin(skin);
            return value == CharacterAvatarOptions.Deep
                || value == CharacterAvatarOptions.Red
                || value == CharacterAvatarOptions.Green
                ? new Color32(255, 246, 236, 255)
                : new Color32(44, 32, 28, 255);
        }

        private static Color AccessoryColor(string accessory)
        {
            string value = CharacterAvatarId.NormalizeAccessory(accessory);

            if (value == CharacterAvatarOptions.Crown || value == CharacterAvatarOptions.Halo)
            {
                return new Color32(255, 211, 106, 255);
            }

            if (value == CharacterAvatarOptions.Cap)
            {
                return new Color32(36, 79, 158, 255);
            }

            if (value == CharacterAvatarOptions.Mask)
            {
                return new Color32(232, 247, 255, 255);
            }

            return new Color32(20, 30, 38, 255);
        }

        private static string InitialFor(string username)
        {
            return string.IsNullOrWhiteSpace(username) ? "" : username.Trim().Substring(0, 1).ToUpperInvariant();
        }

        private static Sprite LoadArtSprite(string species)
        {
            if (artSpriteCache.TryGetValue(species, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>("AvatarArt/" + species);
            if (texture == null)
            {
                artSpriteCache[species] = null;
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(texture.width, texture.height)
            );
            artSpriteCache[species] = sprite;
            return sprite;
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image != null)
            {
                image.sprite = sprite;
            }
        }

        private static void SetImageColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetImageVisible(Image image, bool visible)
        {
            if (image != null)
            {
                image.enabled = visible;
            }
        }

        private static void PositionLimb(Image image, Vector2 position, Vector2 size, float zRotation)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = true;
            image.sprite = RoundedSprite();
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localEulerAngles = new Vector3(0, 0, zRotation);
        }

        private static void SetText(Text label, string value, Color color)
        {
            if (label == null)
            {
                return;
            }

            label.text = value;
            label.color = color;
        }

        private static Color Darken(Color color, float multiplier)
        {
            return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
        }

        private static Color Lighten(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        private static Color OverlayColor(Color color, byte alpha)
        {
            return new Color(color.r, color.g, color.b, alpha / 255f);
        }

        private static Color CheekColorForSkin(Color skinColor)
        {
            Color blush = new Color32(237, 118, 118, 120);
            return Color.Lerp(skinColor, blush, 0.42f);
        }

        private static Sprite CircleSprite()
        {
            if (circleSprite == null)
            {
                circleSprite = CreateShapeSprite(IsCirclePixel);
            }

            return circleSprite;
        }

        private static Sprite RoundedSprite()
        {
            if (roundedSprite == null)
            {
                roundedSprite = CreateShapeSprite(IsRoundedPixel);
            }

            return roundedSprite;
        }

        private static Sprite DiamondSprite()
        {
            if (diamondSprite == null)
            {
                diamondSprite = CreateShapeSprite(IsDiamondPixel);
            }

            return diamondSprite;
        }

        // These procedural masks keep the avatar asset-free while giving it character-like silhouettes.
        private static Sprite FaceSprite()
        {
            if (faceSprite == null)
            {
                faceSprite = CreateShapeSprite(IsFacePixel);
            }

            return faceSprite;
        }

        private static Sprite LongFaceSprite()
        {
            if (longFaceSprite == null)
            {
                longFaceSprite = CreateShapeSprite(IsLongFacePixel);
            }

            return longFaceSprite;
        }

        private static Sprite HairCapSprite()
        {
            if (hairCapSprite == null)
            {
                hairCapSprite = CreateShapeSprite(IsHairCapPixel);
            }

            return hairCapSprite;
        }

        private static Sprite SpikyHairSprite()
        {
            if (spikyHairSprite == null)
            {
                spikyHairSprite = CreateShapeSprite(IsSpikyHairPixel);
            }

            return spikyHairSprite;
        }

        private static Sprite TorsoSprite()
        {
            if (torsoSprite == null)
            {
                torsoSprite = CreateShapeSprite(IsTorsoPixel);
            }

            return torsoSprite;
        }

        private static Sprite LegSprite()
        {
            if (legSprite == null)
            {
                legSprite = CreateShapeSprite(IsLegPixel);
            }

            return legSprite;
        }

        private static Sprite ShoeSprite()
        {
            if (shoeSprite == null)
            {
                shoeSprite = CreateShapeSprite(IsShoePixel);
            }

            return shoeSprite;
        }

        private static Sprite SkirtSprite()
        {
            if (skirtSprite == null)
            {
                skirtSprite = CreateShapeSprite(IsSkirtPixel);
            }

            return skirtSprite;
        }

        private static Sprite CreateShapeSprite(System.Func<int, int, bool> containsPixel)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = containsPixel(x, y)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool IsCirclePixel(int x, int y)
        {
            const float center = 47.5f;
            float dx = x - center;
            float dy = y - center;
            return dx * dx + dy * dy <= center * center;
        }

        private static bool IsRoundedPixel(int x, int y)
        {
            const int radius = 18;
            int clampedX = Mathf.Clamp(x, radius, 95 - radius);
            int clampedY = Mathf.Clamp(y, radius, 95 - radius);
            int dx = x - clampedX;
            int dy = y - clampedY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool IsDiamondPixel(int x, int y)
        {
            return Mathf.Abs(x - 48) + Mathf.Abs(y - 48) <= 48;
        }

        private static bool IsFacePixel(int x, int y)
        {
            float t = y / 95f;
            float halfWidth = t < 0.35f
                ? Mathf.Lerp(18f, 41f, t / 0.35f)
                : t < 0.78f
                    ? 41f
                    : Mathf.Lerp(41f, 34f, (t - 0.78f) / 0.22f);
            float centerX = 47.5f;
            float dx = Mathf.Abs(x - centerX);
            float topRound = Mathf.Clamp01((95f - y) / 16f);
            return dx <= halfWidth - (1f - topRound) * 5f && y > 2;
        }

        private static bool IsLongFacePixel(int x, int y)
        {
            float t = y / 95f;
            float halfWidth = t < 0.42f
                ? Mathf.Lerp(15f, 34f, t / 0.42f)
                : t < 0.82f
                    ? 35f
                    : Mathf.Lerp(35f, 29f, (t - 0.82f) / 0.18f);
            return Mathf.Abs(x - 47.5f) <= halfWidth && y > 2;
        }

        private static bool IsHairCapPixel(int x, int y)
        {
            float t = y / 95f;
            float dx = Mathf.Abs(x - 47.5f);
            float halfWidth = t < 0.22f
                ? Mathf.Lerp(28f, 45f, t / 0.22f)
                : t < 0.75f
                    ? 45f
                    : Mathf.Lerp(45f, 34f, (t - 0.75f) / 0.25f);
            bool cap = dx <= halfWidth && y >= 12;
            bool lowBangs = y < 26 && (x < 33 || x > 63 || Mathf.Abs(x - 48) < 11);
            return cap || lowBangs;
        }

        private static bool IsSpikyHairPixel(int x, int y)
        {
            float dx = Mathf.Abs(x - 48f);
            bool baseCap = y < 58 && y > 9 && dx < Mathf.Lerp(44f, 28f, y / 58f);
            bool centerSpike = Mathf.Abs(x - 48) + Mathf.Abs(y - 72) < 25;
            bool leftSpike = Mathf.Abs(x - 24) + Mathf.Abs(y - 62) < 21;
            bool rightSpike = Mathf.Abs(x - 72) + Mathf.Abs(y - 62) < 21;
            return baseCap || centerSpike || leftSpike || rightSpike;
        }

        private static bool IsTorsoPixel(int x, int y)
        {
            float t = y / 95f;
            float halfWidth = Mathf.Lerp(31f, 45f, t);
            bool shoulderRound = y < 86 || Mathf.Abs(x - 48f) < Mathf.Lerp(34f, 12f, (y - 86f) / 9f);
            return Mathf.Abs(x - 48f) <= halfWidth && y > 3 && shoulderRound;
        }

        private static bool IsLegPixel(int x, int y)
        {
            float t = y / 95f;
            float halfWidth = Mathf.Lerp(15f, 19f, t);
            return Mathf.Abs(x - 48f) <= halfWidth && y > 2;
        }

        private static bool IsShoePixel(int x, int y)
        {
            float dx = Mathf.Abs(x - 50f);
            float dy = Mathf.Abs(y - 42f);
            bool sole = dx < 43f && dy < 20f;
            bool toe = (x > 48 && (x - 48) * (x - 48) / 48f + dy * dy / 16f < 28f);
            return sole || toe;
        }

        private static bool IsSkirtPixel(int x, int y)
        {
            float t = y / 95f;
            float halfWidth = Mathf.Lerp(47f, 27f, t);
            return Mathf.Abs(x - 48f) <= halfWidth && y > 2;
        }
    }
}
