using System;

namespace SaferTogether.UnityClient
{
    // avatar ids shared between the web app and unity
    public static class AvatarIds
    {
        public const string Aqua = "aqua";
        public const string Mint = "mint";
        public const string Sun = "sun";
        public const string Rose = "rose";
        public const string Violet = "violet";
        public const string Steel = "steel";

        public static readonly string[] All =
        {
            Aqua,
            Mint,
            Sun,
            Rose,
            Violet,
            Steel
        };
    }

    // builder avatar option ids, shared with the backend
    public static class AvatarBuilderOptions
    {
        public const string Circle = "circle";
        public const string Square = "square";
        public const string Diamond = "diamond";
        public const string Hex = "hex";

        public const string Dot = "dot";
        public const string Line = "line";
        public const string Happy = "happy";
        public const string Wink = "wink";

        public static readonly string[] Shapes =
        {
            Circle,
            Square,
            Diamond,
            Hex
        };

        public static readonly string[] Eyes =
        {
            Dot,
            Line,
            Happy,
            Wink
        };

        public static readonly string[] Colors =
        {
            AvatarIds.Aqua,
            AvatarIds.Mint,
            AvatarIds.Sun,
            AvatarIds.Rose,
            AvatarIds.Violet,
            AvatarIds.Steel,
            "coral",
            "lime",
            "sky",
            "peach"
        };
    }

    // a composed avatar, held in memory
    [Serializable]
    public class AvatarBuilderSpec
    {
        public string shape;
        public string baseColor;
        public string accentColor;
        public string eyes;
    }

    // helpers for builder avatar ids
    public static class AvatarBuilderId
    {
        // build a clean builder avatar id string
        public static string Build(string shape, string baseColor, string accentColor, string eyes)
        {
            return "builder:" + NormalizeShape(shape)
                + ":" + NormalizeColor(baseColor)
                + ":" + NormalizeColor(accentColor)
                + ":" + NormalizeEyes(eyes);
        }

        // try to turn a builder id back into a spec
        public static bool TryParse(string avatar, out AvatarBuilderSpec spec)
        {
            spec = null;
            string value = NormalizeValue(avatar);

            if (!value.StartsWith("builder:"))
            {
                return false;
            }

            string[] parts = value.Split(':');

            if (parts.Length != 5)
            {
                return false;
            }

            string shape = parts[1];
            string baseColor = parts[2];
            string accentColor = parts[3];
            string eyes = parts[4];

            if (!IsShape(shape) || !IsColor(baseColor) || !IsColor(accentColor) || !IsEyes(eyes))
            {
                return false;
            }

            spec = new AvatarBuilderSpec
            {
                shape = shape,
                baseColor = baseColor,
                accentColor = accentColor,
                eyes = eyes
            };
            return true;
        }

        // turn an old preset id into a default builder spec
        public static AvatarBuilderSpec LegacyToSpec(string avatar)
        {
            string preset = NormalizePreset(avatar);

            return new AvatarBuilderSpec
            {
                shape = AvatarBuilderOptions.Circle,
                baseColor = preset,
                accentColor = AvatarIds.Steel,
                eyes = AvatarBuilderOptions.Dot
            };
        }

        // get a spec from either a builder id or a legacy one
        public static AvatarBuilderSpec ToSpec(string avatar)
        {
            AvatarBuilderSpec parsed;

            if (TryParse(avatar, out parsed))
            {
                return parsed;
            }

            return LegacyToSpec(avatar);
        }

        // clean up a shape id, fall back if it's weird
        public static string NormalizeShape(string shape)
        {
            string value = NormalizeValue(shape);
            return IsShape(value) ? value : AvatarBuilderOptions.Circle;
        }

        // clean up a color id, fall back if it's weird
        public static string NormalizeColor(string color)
        {
            string value = NormalizeValue(color);
            return IsColor(value) ? value : AvatarIds.Aqua;
        }

        // clean up an eyes id, fall back if it's weird
        public static string NormalizeEyes(string eyes)
        {
            string value = NormalizeValue(eyes);
            return IsEyes(value) ? value : AvatarBuilderOptions.Dot;
        }

        // clean up an old preset id, fall back to aqua
        public static string NormalizePreset(string avatar)
        {
            string value = NormalizeValue(avatar);

            foreach (string option in AvatarIds.All)
            {
                if (option == value)
                {
                    return value;
                }
            }

            return AvatarIds.Aqua;
        }

        // is this a legit shape id?
        private static bool IsShape(string value)
        {
            foreach (string option in AvatarBuilderOptions.Shapes)
            {
                if (option == value)
                {
                    return true;
                }
            }

            return false;
        }

        // is this a legit color id?
        private static bool IsColor(string value)
        {
            foreach (string option in AvatarBuilderOptions.Colors)
            {
                if (option == value)
                {
                    return true;
                }
            }

            return false;
        }

        // is this a legit eyes id?
        private static bool IsEyes(string value)
        {
            foreach (string option in AvatarBuilderOptions.Eyes)
            {
                if (option == value)
                {
                    return true;
                }
            }

            return false;
        }

        // trim + lowercase, handles null/empty
        private static string NormalizeValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Trim().ToLowerInvariant();
        }
    }

    // character avatar option ids, shared with the backend
    public static class CharacterAvatarOptions
    {
        public const string Human = "human";
        public const string Dragon = "dragon";
        public const string Bear = "bear";
        public const string Elephant = "elephant";
        public const string Devil = "devil";
        public const string Angel = "angel";

        public const string Female = "female";
        public const string Male = "male";

        public const string Porcelain = "porcelain";
        public const string Light = "light";
        public const string Tan = "tan";
        public const string Brown = "brown";
        public const string Deep = "deep";
        public const string Green = "green";
        public const string Red = "red";
        public const string Gray = "gray";
        public const string Gold = "gold";

        public const string Round = "round";
        public const string Soft = "soft";
        public const string Sharp = "sharp";
        public const string Snout = "snout";
        public const string LongFace = "long";

        public const string Dot = "dot";
        public const string Almond = "almond";
        public const string Happy = "happy";
        public const string Focused = "focused";
        public const string Sleepy = "sleepy";

        public const string EyeBrown = "brown";
        public const string EyeBlue = "blue";
        public const string EyeGreen = "green";
        public const string Hazel = "hazel";
        public const string EyeViolet = "violet";
        public const string Amber = "amber";
        public const string EyeGray = "gray";

        public const string Short = "short";
        public const string Bob = "bob";
        public const string Curls = "curls";
        public const string Spiky = "spiky";
        public const string LongHair = "long";
        public const string Ponytail = "ponytail";
        public const string Bun = "bun";
        public const string Mohawk = "mohawk";
        public const string Hijab = "hijab";
        public const string NoHair = "none";

        public const string Black = "black";
        public const string HairBrown = "brown";
        public const string Blonde = "blonde";
        public const string HairRed = "red";
        public const string HairBlue = "blue";
        public const string Pink = "pink";
        public const string Silver = "silver";
        public const string White = "white";

        public const string Tee = "tee";
        public const string Shirt = "shirt";
        public const string Hoodie = "hoodie";
        public const string Sweatshirt = "sweatshirt";
        public const string Undershirt = "undershirt";
        public const string Jacket = "jacket";
        public const string Vest = "vest";
        public const string Armor = "armor";
        public const string Dress = "dress";

        public const string Jeans = "jeans";
        public const string Training = "training";
        public const string Shorts = "shorts";
        public const string Skirt = "skirt";
        public const string Cargo = "cargo";
        public const string SportsPants = "sports";
        public const string Leggings = "leggings";

        public const string Sneakers = "sneakers";
        public const string Boots = "boots";
        public const string Sandals = "sandals";
        public const string Slippers = "slippers";
        public const string SpaceShoes = "space-shoes";
        public const string NoShoes = "none";

        public const string NoAccessory = "none";
        public const string Glasses = "glasses";
        public const string Cap = "cap";
        public const string Crown = "crown";
        public const string Mask = "mask";
        public const string Bandana = "bandana";
        public const string Headphones = "headphones";
        public const string Wings = "wings";
        public const string Halo = "halo";
        public const string Horns = "horns";
        public const string Tail = "tail";

        public const string Coral = "coral";
        public const string Lime = "lime";
        public const string Sky = "sky";
        public const string Peach = "peach";
        public const string Navy = "navy";
        public const string Denim = "denim";
        public const string Blue = "blue";
        public const string Yellow = "yellow";

        public static readonly string[] Species =
        {
            Male,
            Female,
            Dragon,
            Devil
        };

        public static readonly string[] Sexes =
        {
            Female,
            Male
        };

        public static readonly string[] SkinTones =
        {
            Porcelain,
            Light,
            Tan,
            Brown,
            Deep,
            Green,
            Red,
            Gray,
            Gold
        };

        public static readonly string[] FaceShapes =
        {
            Round,
            Soft,
            Sharp,
            Snout,
            LongFace
        };

        public static readonly string[] Eyes =
        {
            Dot,
            Almond,
            Happy,
            Focused,
            Sleepy
        };

        public static readonly string[] EyeColors =
        {
            EyeBrown,
            EyeBlue,
            EyeGreen,
            Hazel,
            EyeViolet,
            Amber,
            EyeGray
        };

        public static readonly string[] HairStyles =
        {
            Short,
            Bob,
            Curls,
            Spiky,
            LongHair,
            Ponytail,
            Bun,
            Mohawk,
            Hijab,
            NoHair
        };

        public static readonly string[] HairColors =
        {
            Black,
            HairBrown,
            Blonde,
            HairRed,
            HairBlue,
            Pink,
            Silver,
            White
        };

        public static readonly string[] Tops =
        {
            Tee,
            Sweatshirt,
            Undershirt
        };

        public static readonly string[] Bottoms =
        {
            Jeans,
            Cargo,
            SportsPants
        };

        public static readonly string[] Shoes =
        {
            Sneakers,
            Boots,
            SpaceShoes
        };

        public static readonly string[] Accessories =
        {
            NoAccessory,
            Crown,
            Bandana,
            Glasses,
            Mask,
        };

        public static readonly string[] Colors =
        {
            AvatarIds.Aqua,
            AvatarIds.Mint,
            AvatarIds.Sun,
            AvatarIds.Rose,
            AvatarIds.Violet,
            AvatarIds.Steel,
            Coral,
            Lime,
            Sky,
            Peach,
            Navy,
            White,
            Black,
            Blue,
            Red,
            Green,
            Yellow,
            Denim
        };

        public static readonly string[] Backgrounds = Colors;
        public static readonly string[] ClothingColors =
        {
            Black,
            Blue,
            Green,
            Red,
            White,
            Yellow
        };
    }

    // a full character avatar, held in memory
    [Serializable]
    public class CharacterAvatarSpec
    {
        public string species;
        public string sex;
        public string skin;
        public string face;
        public string eyes;
        public string eyeColor;
        public string hair;
        public string hairColor;
        public string top;
        public string topColor;
        public string bottom;
        public string bottomColor;
        public string shoes;
        public string shoeColor;
        public string accessory;
        public string background;
    }

    // helpers for full character avatar ids
    public static class CharacterAvatarId
    {
        // build a clean character:v2 id string
        public static string Build(
            string species,
            string sex,
            string skin,
            string face,
            string eyes,
            string eyeColor,
            string hair,
            string hairColor,
            string top,
            string topColor,
            string bottom,
            string bottomColor,
            string shoes,
            string shoeColor,
            string accessory,
            string background
        )
        {
            return "character:v2:"
                + NormalizeSpecies(species)
                + ":" + NormalizeSex(sex)
                + ":" + NormalizeSkin(skin)
                + ":" + NormalizeFace(face)
                + ":" + NormalizeEyes(eyes)
                + ":" + NormalizeEyeColor(eyeColor)
                + ":" + NormalizeHair(hair)
                + ":" + NormalizeHairColor(hairColor)
                + ":" + NormalizeTop(top)
                + ":" + NormalizeColor(topColor)
                + ":" + NormalizeBottom(bottom)
                + ":" + NormalizeColor(bottomColor)
                + ":" + NormalizeShoes(shoes)
                + ":" + NormalizeColor(shoeColor)
                + ":" + NormalizeAccessory(accessory)
                + ":" + NormalizeColor(background);
        }

        // try to turn a character:v2 id back into a spec
        public static bool TryParse(string avatar, out CharacterAvatarSpec spec)
        {
            spec = null;
            string value = NormalizeValue(avatar);

            if (!value.StartsWith("character:v2:"))
            {
                return false;
            }

            string[] parts = value.Split(':');

            if (parts.Length != 18)
            {
                return false;
            }

            string normalized = Build(
                parts[2],
                parts[3],
                parts[4],
                parts[5],
                parts[6],
                parts[7],
                parts[8],
                parts[9],
                parts[10],
                parts[11],
                parts[12],
                parts[13],
                parts[14],
                parts[15],
                parts[16],
                parts[17]
            );

            string legacyHumanValue = normalized.Replace(":v2:" + CharacterAvatarOptions.Male + ":", ":v2:" + CharacterAvatarOptions.Human + ":");

            if (normalized != value && legacyHumanValue != value)
            {
                return false;
            }

            spec = new CharacterAvatarSpec
            {
                species = NormalizeSpecies(parts[2]),
                sex = NormalizeSex(parts[3]),
                skin = NormalizeSkin(parts[4]),
                face = NormalizeFace(parts[5]),
                eyes = NormalizeEyes(parts[6]),
                eyeColor = NormalizeEyeColor(parts[7]),
                hair = NormalizeHair(parts[8]),
                hairColor = NormalizeHairColor(parts[9]),
                top = NormalizeTop(parts[10]),
                topColor = NormalizeColor(parts[11]),
                bottom = NormalizeBottom(parts[12]),
                bottomColor = NormalizeColor(parts[13]),
                shoes = NormalizeShoes(parts[14]),
                shoeColor = NormalizeColor(parts[15]),
                accessory = NormalizeAccessory(parts[16]),
                background = NormalizeColor(parts[17])
            };
            return true;
        }

        // get a full spec from any old format (preset/builder/v1)
        public static CharacterAvatarSpec ToSpec(string avatar)
        {
            CharacterAvatarSpec characterSpec;

            if (TryParse(avatar, out characterSpec))
            {
                return characterSpec;
            }

            if (TryParseLegacy(avatar, out characterSpec))
            {
                return characterSpec;
            }

            AvatarBuilderSpec builderSpec = AvatarBuilderId.ToSpec(avatar);

            return new CharacterAvatarSpec
            {
                species = CharacterAvatarOptions.Male,
                sex = CharacterAvatarOptions.Male,
                skin = CharacterAvatarOptions.Tan,
                face = CharacterAvatarOptions.Soft,
                eyes = builderSpec.eyes == AvatarBuilderOptions.Wink
                    ? CharacterAvatarOptions.Focused
                    : CharacterAvatarOptions.Happy,
                eyeColor = CharacterAvatarOptions.EyeBrown,
                hair = CharacterAvatarOptions.Short,
                hairColor = CharacterAvatarOptions.HairBrown,
                top = CharacterAvatarOptions.Tee,
                topColor = NormalizeColor(builderSpec.baseColor),
                bottom = CharacterAvatarOptions.Jeans,
                bottomColor = CharacterAvatarOptions.Denim,
                shoes = CharacterAvatarOptions.Sneakers,
                shoeColor = CharacterAvatarOptions.Black,
                accessory = CharacterAvatarOptions.NoAccessory,
                background = NormalizeColor(builderSpec.accentColor)
            };
        }

        // parse the old character:v1 ids so they still work
        private static bool TryParseLegacy(string avatar, out CharacterAvatarSpec spec)
        {
            spec = null;
            string value = NormalizeValue(avatar);

            if (!value.StartsWith("character:v1:"))
            {
                return false;
            }

            string[] parts = value.Split(':');

            if (parts.Length != 11)
            {
                return false;
            }

            string skin = NormalizeLegacySkin(parts[2]);
            string hair = NormalizeLegacyHair(parts[3]);
            string hairColor = NormalizeLegacyHairColor(parts[4]);
            string top = NormalizeLegacyTop(parts[5]);
            string topColor = NormalizeColor(parts[6]);
            string eyes = NormalizeLegacyEyes(parts[7]);
            string accessory = NormalizeLegacyAccessory(parts[9]);
            string background = NormalizeColor(parts[10]);

            spec = new CharacterAvatarSpec
            {
                species = CharacterAvatarOptions.Male,
                sex = CharacterAvatarOptions.Male,
                skin = skin,
                face = CharacterAvatarOptions.Soft,
                eyes = eyes,
                eyeColor = CharacterAvatarOptions.EyeBrown,
                hair = hair,
                hairColor = hairColor,
                top = top,
                topColor = topColor,
                bottom = CharacterAvatarOptions.Jeans,
                bottomColor = CharacterAvatarOptions.Denim,
                shoes = CharacterAvatarOptions.Sneakers,
                shoeColor = CharacterAvatarOptions.Black,
                accessory = accessory,
                background = background
            };
            return true;
        }

        // clean up species, map old "human" to male
        public static string NormalizeSpecies(string value)
        {
            string cleanValue = NormalizeValue(value);
            if (cleanValue == CharacterAvatarOptions.Human)
            {
                return CharacterAvatarOptions.Male;
            }

            return NormalizeOption(cleanValue, CharacterAvatarOptions.Species, CharacterAvatarOptions.Male);
        }

        // clean up sex, defaults female
        public static string NormalizeSex(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.Sexes, CharacterAvatarOptions.Female);
        }

        // clean up skin tone, defaults tan
        public static string NormalizeSkin(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.SkinTones, CharacterAvatarOptions.Tan);
        }

        // clean up face shape, defaults soft
        public static string NormalizeFace(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.FaceShapes, CharacterAvatarOptions.Soft);
        }

        // clean up eyes, defaults almond
        public static string NormalizeEyes(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.Eyes, CharacterAvatarOptions.Almond);
        }

        // clean up eye color, defaults brown
        public static string NormalizeEyeColor(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.EyeColors, CharacterAvatarOptions.EyeBrown);
        }

        // clean up hair style, defaults short
        public static string NormalizeHair(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.HairStyles, CharacterAvatarOptions.Short);
        }

        // clean up hair color, defaults brown
        public static string NormalizeHairColor(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.HairColors, CharacterAvatarOptions.HairBrown);
        }

        // clean up top, defaults tee
        public static string NormalizeTop(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.Tops, CharacterAvatarOptions.Tee);
        }

        // clean up bottom, defaults jeans
        public static string NormalizeBottom(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.Bottoms, CharacterAvatarOptions.Jeans);
        }

        // clean up shoes, defaults sneakers
        public static string NormalizeShoes(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.Shoes, CharacterAvatarOptions.Sneakers);
        }

        // clean up accessory, defaults none
        public static string NormalizeAccessory(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.Accessories, CharacterAvatarOptions.NoAccessory);
        }

        // clean up a color, defaults aqua
        public static string NormalizeColor(string value)
        {
            return NormalizeOption(value, CharacterAvatarOptions.Colors, AvatarIds.Aqua);
        }

        // map an old skin id onto the new ones
        private static string NormalizeLegacySkin(string value)
        {
            return NormalizeOption(value, new[] { CharacterAvatarOptions.Light, CharacterAvatarOptions.Tan, CharacterAvatarOptions.Brown, CharacterAvatarOptions.Deep }, CharacterAvatarOptions.Tan);
        }

        // map an old hair id onto the new ones
        private static string NormalizeLegacyHair(string value)
        {
            return NormalizeOption(value, new[] { CharacterAvatarOptions.Short, CharacterAvatarOptions.Bob, CharacterAvatarOptions.Curls, CharacterAvatarOptions.Spiky, CharacterAvatarOptions.Hijab, CharacterAvatarOptions.NoHair }, CharacterAvatarOptions.Short);
        }

        // map an old hair color onto the new ones
        private static string NormalizeLegacyHairColor(string value)
        {
            return NormalizeOption(value, new[] { CharacterAvatarOptions.Black, CharacterAvatarOptions.HairBrown, CharacterAvatarOptions.Blonde, CharacterAvatarOptions.HairRed, CharacterAvatarOptions.HairBlue, CharacterAvatarOptions.Silver }, CharacterAvatarOptions.HairBrown);
        }

        // map an old top id onto the new ones
        private static string NormalizeLegacyTop(string value)
        {
            return NormalizeOption(value, new[] { CharacterAvatarOptions.Tee, CharacterAvatarOptions.Hoodie, CharacterAvatarOptions.Jacket, CharacterAvatarOptions.Vest }, CharacterAvatarOptions.Tee);
        }

        // map old eyes onto the new ones (line -> sleepy)
        private static string NormalizeLegacyEyes(string value)
        {
            string cleanValue = NormalizeValue(value);

            if (cleanValue == "line")
            {
                return CharacterAvatarOptions.Sleepy;
            }

            return NormalizeOption(cleanValue, new[] { CharacterAvatarOptions.Dot, CharacterAvatarOptions.Happy, CharacterAvatarOptions.Focused }, CharacterAvatarOptions.Almond);
        }

        // map old accessory onto the new ones (badge -> crown)
        private static string NormalizeLegacyAccessory(string value)
        {
            string cleanValue = NormalizeValue(value);

            if (cleanValue == "badge")
            {
                return CharacterAvatarOptions.Crown;
            }

            return NormalizeOption(cleanValue, new[] { CharacterAvatarOptions.NoAccessory, CharacterAvatarOptions.Glasses, CharacterAvatarOptions.Cap, CharacterAvatarOptions.Mask }, CharacterAvatarOptions.NoAccessory);
        }

        // keep the value if it's in the list, else use the fallback
        private static string NormalizeOption(string value, string[] options, string fallback)
        {
            string cleanValue = NormalizeValue(value);
            return Contains(options, cleanValue) ? cleanValue : fallback;
        }

        // is value in the array?
        private static bool Contains(string[] options, string value)
        {
            foreach (string option in options)
            {
                if (option == value)
                {
                    return true;
                }
            }

            return false;
        }

        // trim + lowercase, handles null/empty
        private static string NormalizeValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Trim().ToLowerInvariant();
        }
    }

    // user profile we get from the gateway
    [Serializable]
    public class UserProfile
    {
        public string id;
        public string username;
        public string role;
        public string avatar;
        public string avatarImage;
    }

    // session info the website passes into the embedded unity build
    [Serializable]
    public class WebSessionMessage
    {
        public string gatewayBaseUrl;
        public string returnUrl;
        public string draftAvatar;
        public UserProfile profile;
    }

    // body we send to sign up
    [Serializable]
    public class SignUpRequest
    {
        public string username;
        public string password;
        public string role;
        public string avatar;
    }

    // body we send to log in
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    // body we send to update the avatar
    [Serializable]
    public class AvatarUpdateRequest
    {
        public string avatar;
        public string avatarImage;
    }

    // what signup/login send back
    [Serializable]
    public class AuthResponse
    {
        public string accessToken;
        public UserProfile profile;
    }

    // what the profile endpoints send back
    [Serializable]
    public class ProfileResponse
    {
        public UserProfile profile;
    }

    // what logout sends back
    [Serializable]
    public class LogoutResponse
    {
        public bool success;
    }

    // error shape the gateway sends back
    [Serializable]
    public class ErrorResponse
    {
        public string error;
    }
}
