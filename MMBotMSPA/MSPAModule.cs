using System;
using System.Collections.Generic;
using MMBot;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace MMBotMSPA
{
    public class MSPAModule : BotModule
    {
        public MSPAModule()
        {
            Module1.AddLinkHandler(LinkHandler);
        }

        public override void Shutdown() { }

        private static readonly string[] LandName = {
"Acid",
"Advertisement",
"Age",
"Alloy",
"Alternation",
"Ambiance",
"Amusement",
"Angels",
"Annoyance",
"Apathy",
"Art",
"Ascent",
"Ash",
"Atoll",
"Autumn",
"Backstabbing",
"Bamboo",
"Bastille",
"Beauty",
"Blankness",
"Blaze",
"Boil",
"Bone",
"Books",
"Braille",
"Brains",
"Brick",
"Bridges",
"Bronze",
"Brooks",
"Bubbles",
"Bulge",
"Burlap",
"Butterflies",
"Cacophony",
"Cacti",
"Cages",
"Canopy",
"Canyons",
"Carbon",
"Carpet",
"Cathedrals",
"Caves",
"Chains",
"Change",
"Charge",
"Chemicals",
"Chocolate",
"Circuitry",
"Clay",
"Cliffs",
"Clockwork",
"Clouds",
"Cobalt",
"Cobblestone",
"Concrete",
"Construction",
"Contact",
"Contrast",
"Copper",
"Coral",
"Cotton",
"Crossroads",
"Crystal",
"Cubes",
"Curses",
"Dance",
"Dawn",
"Death",
"Decor",
"Depth",
"Descent",
"Desolation",
"Dew",
"Diamonds",
"Dirt",
"Disease",
"Dismay",
"Dolls",
"Dragons",
"Drought",
"Dungeons",
"Dust",
"Dye",
"Ebony",
"Echo",
"Eldritch",
"Electronics",
"Elegance",
"Emeralds",
"Evergreens",
"Falsehood",
"Fatigue",
"Fear",
"Festivities",
"Fire",
"Flame",
"Flow",
"Flowers",
"Fog",
"Forest",
"Fortification",
"Fossils",
"Freefall",
"Frost",
"Fungi",
"Fur",
"Gardens",
"Germs",
"Ghosts",
"Glamour",
"Glass",
"Glitter",
"Gloom",
"Glow",
"Glue",
"Gold",
"Graphite",
"Grass",
"Graves",
"Gravity",
"Gust",
"Hail",
"Hallucinations",
"Harbors",
"Hats",
"Hay",
"Haze",
"Heat",
"Hedges",
"Henge",
"Hills",
"Holes",
"Holly",
"Horror",
"Ice",
"Illusion",
"Ink",
"Insanity",
"Insomnia",
"Intimidation",
"Iron",
"Irradiation",
"Irrigation",
"Islands",
"Ivory",
"Jail",
"Jazz",
"Jokers",
"Jolliness",
"Jungle",
"Junk",
"Juxtaposition",
"Karaoke",
"Karate",
"Knowledge",
"Labyrinth",
"Ladders",
"Lakes",
"Lasers",
"Laugh",
"Law",
"Laze",
"Leaves",
"Levitation",
"Light",
"Loam",
"Luck",
"Magma",
"Magnets",
"Maps",
"Marsh",
"Meadow",
"Melody",
"Mercury",
"Mirrors",
"Mirth",
"Misers",
"Mist",
"Moss",
"Motion",
"Mountains",
"Mushrooms",
"Nails",
"Neon",
"Nickel",
"Night",
"Noir",
"Noise",
"Oasis",
"Obsidian",
"Odors",
"Oil",
"Opposites",
"Orchard",
"Ore",
"Paper",
"Paths",
"Pipes",
"Pistons",
"Plains",
"Plastic",
"Platforms",
"Pluck",
"Pollution",
"Portals",
"Powder",
"Prairie",
"Precipice",
"Presence",
"Presents",
"Prisms",
"Pulse",
"Pumpkins",
"Pyramids",
"Quakes",
"Quarry",
"Quartz",
"Rain",
"Rainbows",
"Ramps",
"Rays",
"Rebirth",
"Reflection",
"Regret",
"Repetition",
"Reversal",
"Rhythm",
"Rime",
"Rip",
"Rivers",
"Rock",
"Roses",
"Roses",
"Rot",
"Rubber",
"Rust",
"Sadness",
"Sand",
"Savannah",
"Scales",
"Science",
"Sentience",
"Shade",
"Shadow",
"Shattering",
"Shores",
"Shrines",
"Shrouds",
"Silence",
"Silhouette",
"Silk",
"Silt",
"Silver",
"Singe",
"Sketch",
"Skin",
"Sky",
"Slate",
"Slime",
"Sluice",
"Slumber",
"Snakes",
"Snow",
"Solace",
"Song",
"Spheres",
"Spices",
"Spikes",
"Spires",
"Sponge",
"Springs",
"Stability",
"Stairs",
"Static",
"Steam",
"Steel",
"Steppe",
"Storm",
"Strings",
"Stumps",
"Subway",
"Suction",
"Sulfur",
"Surprise",
"Swamp",
"Sweets",
"Tea",
"Teeth",
"Temples",
"Tents",
"Terror",
"Thorns",
"Thought",
"Thunder",
"Tin",
"Tinsel",
"Topiary",
"Trance",
"Tranquility",
"Transit",
"Traps",
"Travel",
"Treasure",
"Trove",
"Tundra",
"Tunnel",
"Turmoil",
"Twilight",
"Ultraviolence",
"Undead",
"Urns",
"Vacuum",
"Vapor",
"Variety",
"Veil",
"Velvet",
"Vibration",
"Vines",
"Walls",
"War",
"Warmth",
"Wasteland",
"Water",
"Waterfalls",
"Waves",
"Wax",
"Wheat",
"Wilderness",
"Wind",
"Windows",
"Womb",
"Wood",
"Wrath",
"Xenon",
"Yarn",
"Yore",
"Zen",
"Zephyr"
};

        private static readonly string[] TitleName1 = {
"Bane",
"Bard",
"Foe",
"Guide",
"Host",
"Mage",
"Monk",
"Page",
"Rogue",
"Sage",
"Spy",
"Sylph",
"Thane",
"Thief",
"Count",
"Duke",
"Heir",
"King",
"Queen",
"Priest",
"Maid",
"Witch",
};

        private static readonly string[] TitleName2 = {
"Bliss",
"Blood",
"Breath",
"Caste",
"Cause",
"Choice",
"Dawn",
"Death",
"Depth",
"Doom",
"Dreams",
"Faith",
"Flesh",
"Form",
"Gale",
"Hate",
"Heart",
"Height",
"Hope",
"Joy",
"Keys",
"Law",
"Life",
"Light",
"Loss",
"Love",
"Might",
"Mind",
"Oath",
"Peace",
"Rage",
"Rhyme",
"Right",
"Self",
"Toil",
"Shape",
"Soul",
"Sound",
"Space",
"Stars",
"Time",
"Touch",
"Truth",
"Verve",
"Vim",
"Void",
"Wealth",
"Zeal",
};

        private static readonly string[] Chum1 = {
"irregular",
"harmonica",
"lonely",
"liberal",
"smiling",
"graphical",
"judicial",
"frequent",
"transparant",
"guilded",
"informative",
"acapella",
"ace",
"adios",
"analog",
"apocalypse",
"aquatic",
"arachnids",
"arcane",
"arduous",
"aristocratic",
"arsenic",
"ballistic",
"basic",
"benign",
"blackout",
"blatant",
"bonsai",
"caligulas",
"cannon",
"carapace",
"carcino",
"centaurs",
"concave",
"cuttlefish",
"dark",
"destitute",
"digital",
"directory",
"double",
"ductile",
"dynamite",
"ecto",
"elegant",
"exasperant",
"forgotten",
"fortuitous",
"fractal",
"future",
"gallows",
"garden",
"ghosty",
"golden",
"golgothas",
"grayscale",
"grim",
"gutsy",
"highway",
"hotblooded",
"hysterical",
"iconic",
"imperfect",
"irrelevant",
"jovial",
"knotted",
"lampost",
"lasting",
"laughable",
"lethargic",
"linguistic",
"malignant",
"matrix",
"mechanical",
"misguided",
"nocturnal",
"notorious",
"octagon",
"oddball",
"outlandish",
"paragon",
"passive",
"petulant",
"pickle",
"pocket",
"problem",
"pseudo",
"queasy",
"racecar",
"rainbow",
"restless",
"retro",
"robust",
"royal",
"saffron",
"sapphire",
"serious",
"silent",
"solar",
"spectral",
"stealthy",
"suspicious",
"temporal",
"tentacle",
"textbook",
"timaeus",
"tipsy",
"tolerant",
"trivial",
"turntech",
"twin",
"uncanny",
"undying",
"unusual",
"uranian",
"vegas",
"vivid",
"western",
"whimsical",
"xenophobic",
"yesterdays",
"zealous"
};

        private static readonly string[] Chum2 = {
"Scribbler",
"Protagonist",
"Countryman",
"Doppelganger",
"Detective",
"Swordsman",
"Regulator",
"Magician",
"Blacksmith",
"Onlooker",
"Acupuncture",
"Adversary",
"Anatomy",
"Apothecary",
"Aquarium",
"Arisen",
"Armageddons",
"Assassin",
"Auxiliatrix",
"Bacteria",
"Barnacle",
"Biologist",
"Calibrator",
"Capitalist",
"Catnip",
"Celebration",
"Culler",
"Derivative",
"Dictator",
"Divulger",
"Educator",
"Enthusiast",
"Equinox",
"Escapist",
"Eternity",
"Explorer",
"Extinguisher",
"Factory",
"Forger",
"Gambler",
"Geneticist",
"Gnostalgic",
"Gnostic",
"Godhead",
"Grip",
"Guitarist",
"Gumshoe",
"Hunter",
"Hyperdrive",
"Illustrator",
"Inferno",
"Infiltrator",
"Inflection",
"Inspector",
"Javelin",
"Journalist",
"Juggernaut",
"Kinesis",
"Kinsmen",
"Luminary",
"Materialist",
"Mobilizer",
"Mobster",
"Monster",
"Nativity",
"Noisemaker",
"Notation",
"Odyssey",
"Olympiad",
"Origins",
"Pacifist",
"Paradox",
"Perfectionist",
"Popsicle",
"Professor",
"Programmer",
"Protector",
"Questioner",
"Quickster",
"Ravager",
"Renegade",
"Robber",
"Rocket",
"Sampler",
"Samurai",
"Scarecrow",
"Scavenger",
"Sentenial",
"Sleuth",
"Smuggler",
"Spaceship",
"Spectacle",
"Taxonomist",
"Testicle",
"Testified",
"Testimony",
"Terror",
"Therapist",
"Toreador",
"Trickster",
"Troubadour",
"Typhoon",
"Umbra",
"Umbrage",
"Usurper",
"Vacancy",
"Vestibule",
"Visionary",
"Warlord",
"Waterfall",
"Wizard",
"Xylophone",
"Youngster",
"Zeppelin"
};

        private static readonly string[] Chum1Canon = {
"adios",
"apocalypse",
"arachnids",
"arsenic",
"caligulas",
"carcino",
"centaurs",
"cuttlefish",
"ecto",
"gallows",
"garden",
"ghosty",
"golgothas",
"grim",
"gutsy",
"tentacle",
"timaeus",
"tipsy",
"turntech",
"twin",
"undying",
"uranian"
};

        private static readonly string[] Chum2Canon = {
"Aquarium",
"Arisen",
"Armageddons",
"Auxiliatrix",
"Biologist",
"Calibrator",
"Catnip",
"Culler",
"Geneticist",
"Gnostalgic",
"Gnostic",
"Godhead",
"Grip",
"Gumshoe",
"Testicle",
"Testified",
"Terror",
"Therapist",
"Toreador",
"Trickster",
"Umbra",
"Umbrage"
};

        private static readonly string[] Interests = {
"Video Games",
"Cooking",
"Botany",
"Art",
"Grimdark Arts",
"Biology",
"Collectibles",
"Science",
"Geology",
"Engineering",
"Frivolity",
"Archaeology",
"Literature",
"Immaturity",
"Sports",
"Warfare",
"Gambling",
"Knick Knacks",
"Paleontology",
"Meta"
};

        private static readonly Dictionary<string, string[]> InterestItems = new Dictionary<string, string[]>() {
{"Video Games", new string[] {"Metroid Prime", "Mario Figurine", "Minecraft", "Castlevania","The You Testament", "The Orange Box", "Shitty Roguelike burned to CD", "Sonic Plushtoy"}},
{"Cooking", new string[] {"Saucepan", "Hot Sauce", "Cooking For Dummies", "Toaster", "Cooking Grease", "Ice Cream"}},
{"Botany", new string[] {"Fertilizer", "Pumpkin", "Amorphophallus titanum", "Venus Fly Trap", "Tree Branch", "Watering Can"}},
{"Art", new string[] {"Paintbrush", "Charcoal", "Pencil", "Creepy Figurine", "MC Escher Picture", "Giger Painting"}},
{"Grimdark Arts", new string[] {"Summoning Grimoire", "Vial of your own blood", "Creepy Figurine", "'Username: 666' video, burned onto DVD", "Lovecraftian Novel", "Incense Holder", "Ectoplasm"}},
{"Biology", new string[] {"Protein", "Crab Claw", "Genetic Horror", "Hazardous Chemicals", "Hologram of your own brain", "Fetus in a jar", "Taxidermied creature"}},
{"Collectibles", new string[] {"Sonic Screwdriver", "Replica Lightsabre", "Tardis", "Dalek Figurine", "Miniature Stargate", "Spock Poster", "Transformer Action Figure", "Life-Sized Replica of the Millennium Falcon"}},
{"Science", new string[] {"Tesla Coil", "Fusion Reactor", "Newton's Cradle", "Steampunk Novel", "Carbon Nanotube", "Vial of Mercury"}},
{"Geology", new string[] {"Obsidian Stone", "Meteorite", "Diamond", "Geode", "Gold Leaf", "Interesting Stones"}},
{"Engineering", new string[] {"Car Engine", "Plane Replica", "Car Battery", "Model Skyscraper"}},
{"Frivolity", new string[] {"Wizard Hat", "Shitty Wizard Figurine", "Creepy Doll", "Trick Cards", "Whoopie Cushion", "Smoke Pellets", "Fake Blood Capsules", "Sassacre's Daunting Tome", "Magician's Gloves", "Clown Poster", "Unicycle", "Jack-in-the-Box", "Can-of-worms"}},
{"Archaeology", new string[] {"Suit of Armor", "Mummy", "Sarcophagus", "Jade Figurine", "Indiana Jones Movie", "Dragon Figurine", "Toy Soldiers"}},
{"Literature", new string[] {"Midsummer Night's Dream", "The Alchemist", "Chinese Haikus of Littler Historical Significance"}},
{"Immaturity", new string[] {"Push-me Popper", "Teddy Bear", "Play-Doh", "Bouncy Ball"}}	,
{"Sports", new string[] {"Football", "Baseball Glove", "Hockey Stick", "Tennis Racket"}},
{"Warfare", new string[] {"Model Tank", "Ghilie Suit", "Night-vision Goggles", "Empty Magazine"}},
{"Gambling", new string[] {"Playing Cards", "Poker Chips", "Roulette Wheel"}},
{"Knick Knacks", new string[] {"Snow Globe", "Lava Lamp", "Miniature Eiffel Tower", "Plasma Globe", "Music Box"}},
{"Paleontology", new string[] {"Fossilized Dinosaur Skin", "Preserved Amber", "Ammonite Shell", "T-Rex Tooth", "Footprint Fossil"}},
{"Meta", new string[] {"SBHJ poster", "Con-Air Bunny", "Cool Horse Painting", "Problem Sleuth Poster", "Sepulchritude Shirt"}},	
};

        private static readonly string[] Specibi = {
"bladeKind",
"pistolKind",
"offcespplyKind",
"gloveKind",
"axeKind",
"fncysntaKind",
"whipKind",
"hammerKind",
"clubKind",
"clawKind",
"chainsawKind",
"makeupKind",
"umbrellaKind",
"scytheKind",
"spearKind",
"wandKind",
"bowKind",
"diceKind",
"2*3dentKind",
"guitarKind",
"knifeKind",
"grenadeKind",
"staffkind",
"riflekind",
"maceKind",
"explosivesKind",
"projectileKind",
"wrenchKind",
"screwdriverKind"
};

        private static readonly Dictionary<string, string[]> SpecibiWeapons = new Dictionary<string, string[]>() {
{"bladeKind", new string[] {"Bread Knife", "Sword", "Model Lightsabre", "2 Handed Sword", "Katana"}},
{"pistolKind", new string[] {"Revolver", "Squirt Gun", "Nerf Gun", "Uzi", "Flare Gun"}},
{"offcespplyKind", new string[] {"Stapler", "Staple Gun", "Ruler", "Scissors"}},
{"gloveKind", new string[] {"Gloves", "Boxing Gloves", "Brass Knuckles"}},
{"axeKind", new string[] {"Fire Axe", "Battleaxe", "Woodcutting Axe"}},
{"fncysntaKind", new string[] {"Fancy Santa"}},
{"whipKind", new string[] {"Dog Leash", "Wiimote", "1/2 bow", "Lasso", "Whip", "Leather Belt"}},
{"hammerKind", new string[] {"Hammer", "Sledgehammer", "War Hammer"}},
{"clubKind", new string[] {"Large Stick", "Baseball Bat", "Golf Club", "Night Stick"}},
{"clawKind", new string[] {"Fake Fingernails", "Wolverine Claws", "Bagh Nakh"}},
{"chainsawKind", new string[] {"Chainsaw"}},
{"makeupKind", new string[] {"Lipstick"}},
{"umbrellaKind", new string[] {"Umbrella"}},
{"scytheKind", new string[] {"Farming Scythe", "Scythe"}},
{"spearKind", new string[] {"Pointy Stick", "Spear", "Javelin", "Hawaiian Sling"}},
{"wandKind", new string[] {"Shitty Wand", "Knitting Needles", "Twig"}},
{"bowKind", new string[] {"Bow & Arrow", "Crossbow"}},
{"diceKind", new string[] {"Pop-o-Matic", "Yahtzee Dice", "8 8-sided Dice"}},
{"2*3dentKind", new string[] {"Trident", "Fork", "Pitchfork"}},
{"guitarKind", new string[] {"Acoustic Guitar", "Electric Guitar", "12-String Guitar", "Ukulele"}},
{"knifeKind", new string[] {"Steak Knife", "Pocket Knife", "Dagger", "Kukiri", "Sai"}},
{"grenadeKind", new string[] {"Frag Grenades", "Smoke Grenades", "Flash Grenades", "Molotov Cocktails"}},
{"staffkind", new string[] {"Pool Cue", "Bo Staff", "Quarterstaff"}},
{"riflekind", new string[] {"Paintball Gun", "Machine Gun", "Shotgun", "Sniper Rifle", "Musket", "Harpoon Gun"}},
{"maceKind", new string[] {"Mace", "Flail", "Morning Star", "Nunchucks"}},
{"explosivesKind", new string[] {"Confetti Poppers", "Fireworks", "Dynamite", "C4", "Claymores"}},
{"projectileKind", new string[] {"Darts", "Throwing Knives", "Shirukens", "Kunai"}},
{"wrenchKind", new string[] {"Crescent Wrench", "Pipe Wrench", "Monkey Wrench", "Socket Wrench"}},
{"screwdriverKind", new string[] {"Flathead Screwdriver", "Phillips Screwdriver", "Power Screwdriver", "Sonic Screwdriver toy"}}
};

        private static readonly string[] ConsortTypes = {
"alligators",
"anglerfish",
"ants",
"axolotls",
"bats",
"bears",
"bluebirds",
"canaries",
"cats",
"cougars",
"crocodiles",
"dogs",
"dolphins",
"doves",
"dragons",
"dugons",
"elephants",
"ferrets",
"foxes",
"gargoyles",
"giraffes",
"hamsters",
"humanimals",
"igaunas",
"kangaroos",
"lions",
"manatees",
"manticores",
"mice",
"minxes",
"monkeys",
"otters",
"owls",
"pandas",
"panthers",
"parrots",
"penguins",
"pigs",
"ponies",
"pterodactyls",
"rats",
"rhinos",
"salamanders",
"seagulls",
"sharks",
"snakes",
"squids",
"tigers",
"toucans",
"unicorns",
"velociraptors",
"vultures",
"walruses",
"whales",
"wolves",
"pandas",
"hawks",
"geckos",
"lemurs",
"chameleons",
"chickens",
"rabbits",
"weasels",
"griffons",
"badgers",
"snakes",
"beavers",
"cranes",
"skunks",
"hippos",
"turtles",
"sheep",
"racoons",
"ducks",
"moose",
"cows",
"camels"
};

        private static readonly string[] ConsortColors = {
"black",
"blue",
"fuschia",
"gold",
"green",
"hot pink",
"indigo",
"lavender",
"midnight blue",
"multi-colored",
"olive green",
"orange",
"pearl white",
"pink",
"red",
"silver",
"sky blue",
"steel grey",
"teal",
"violet",
"whatpumpkin orange",
"white",
"yellow",
"forest green",
"translucent",
"vermillion",
"turquoise",
"cerulean",
"rainbow",
"cyan",
"magenta",
"mustard yellow",
"neon yellow",
"brown",
"beige"
};

        private static readonly string[] ConsortQuirks = {
"arrogant",
"awesome",
"depressed",
"easily distracted",
"eccentric",
"gullible",
"hyperactive",
"insulting",
"irrational",
"joyful",
"lazy",
"mysterious",
"shy",
"stupid",
"violent",
"warlike",
"weird",
"naive",
"timid",
"rambunctious",
"swindling",
"helpful",
"brazen",
"neurotic",
"impressionable",
"blithe",
"forgetful",
"uncoordinated"
};

        private static readonly string[] ConsortLikes = {
"architecture",
"art",
"beauty",
"friendship",
"gadgetry",
"knowledge",
"magic",
"mining",
"money",
"nature",
"politics",
"power",
"skateboarding",
"stories",
"tomfoolery",
"gemstones",
"science",
"exploration",
"adventure",
"agriculture",
"pranks",
"music",
"dancing",
"swimming",
"philosophy"
};

        public static string[] HomestuckCharacters = {
                                                     "John Egbert",
                                                     "Rose Lalonde",
                                                     "Dave Strider",
                                                     "Jade Harley",
                                                     "Jane Crocker",
                                                     "Jake English",
                                                     "Roxy Lalonde",
                                                     "Dirk Strider",
                                                     "Dad",
                                                     "Mom",
                                                     "Nanna Egbert",
                                                     "Poppop Crocker",
                                                     "Bro",
                                                     "Grandma English",
                                                     "Grandpa Harley",
                                                     "Jaspers",
                                                     "Becquerel",
                                                     "GCat",
                                                     "Davesprite",
                                                     "Jadesprite",
                                                     "Wayward Vagabond",
                                                     "Peregrine Mendicant",
                                                     "Authority Regulator",
                                                     "Windswept Questant",
                                                     "Writ Keeper",
                                                     "Casey",
                                                     "Secret Wizard",
                                                     "Crumplehat",
                                                     "Vodka Mutini",
                                                     "Liv Tyler",
                                                     "Maplehoof",
                                                     "Squarewave",
                                                     "Sawtooth",
                                                     "Autoresponder",
                                                     "White Queen",
                                                     "White King",
                                                     "Black Queen",
                                                     "Black King",
                                                     "Jack Noir",
                                                     "Draconian Dignitary",
                                                     "Hegemonic Brute",
                                                     "Courtyard Droll",
                                                     "Spades Slick",
                                                     "Diamonds Droog",
                                                     "Hearts Boxcars",
                                                     "Clubs Deuce",
                                                     "Bec Noir",
                                                     "Karkat Vantas",
                                                     "Aradia Megido",
                                                     "Tavros Nitram",
                                                     "Sollux Captor",
                                                     "Nepeta Leijon",
                                                     "Kanaya Maryam",
                                                     "Terezi Pyrope",
                                                     "Vriska Serket",
                                                     "Equius Zahhak",
                                                     "Gamzee Makara",
                                                     //"Eridan Ampora",
                                                     "Feferi Peixes",
                                                     "Sufferer",
                                                     "Handmaid",
                                                     "Summoner",
                                                     "\u03A8iioniic",
                                                     "Disciple",
                                                     "Dolorosa",
                                                     "Redglare",
                                                     "Mindfang",
                                                     "Expatriate",
                                                     "Highblood",
                                                     "Dualscar",
                                                     ")(IC",
                                                     Module1.ColorChar + "09Itchy" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Doze" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Trace" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Clover" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Fin" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Die" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Crowbar" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Sn" + Module1.ColorChar + "01o" + Module1.ColorChar + "09wman" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Stitch" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Sawbuck" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Matchsticks" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Eggs" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Bisquits" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Quarters" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Cans" + Module1.ColorChar,
                                                     Module1.ColorChar + "09D" + Module1.ColorChar + "00o" + Module1.ColorChar + "09c Scratch" + Module1.ColorChar,
                                                     Module1.ColorChar + "09Lord English" + Module1.ColorChar,
                                                     "Lil' Cal",
                                                     "Calliope",
                                                     "Caliborn",
                                                     "Colonel Sassacre",
                                                     "Halley",
                                                     "Serenity",
                                                     "MSPA Reader",
                                                     "Ms. Paint",
                                                     "Andrew Hussie",
                                                     "Tavrisprite",
                                                     "Fefetasprite",
                                                     "ARquiusprite",
                                                     "Erisolsprite",
                                                     "Damara Megido",
                                                     "Rufioh Nitram",
                                                     "Mituna Captor",
                                                     "Meulin Leijon",
                                                     "Kankri Vantas",
                                                     "Porrim Maryam",
                                                     "Latula Pyrope",
                                                     "Aranea Serket",
                                                     "Horuss Zahhak",
                                                     "Kurloz Makara",
                                                     //"Cronus Ampora",
                                                     "Meenah Peixes",
                                                     "Crabdad",
                                                     "Aurthour",
                                                     "Pounce de Leon"
                                                 };

        public static string[] ProblemSleuthCharacters = {
                                                         "Problem Sleuth",
                                                         "Ace Dick",
                                                         "Pickle Inspector",
                                                         "Mobster Kingpin",
                                                         "Nervous Broad",
                                                         "Hysterical Dame",
                                                         "Mannerly Highbrow",
                                                         "Dapper Swain",
                                                         "Churlish Toff",
                                                         "Zombie Ace Dick",
                                                         "Fiesta Ace Dick",
                                                         "Wifehearst",
                                                         "Sonhearst",
                                                         "Porkhearst",
                                                         "Weasel King",
                                                         "Madame Murel",
                                                         "Fluthlu",
                                                         "Morthol Dryax",
                                                         "Hired Muscle",
                                                         "Demimonde Goddess",
                                                         "Godhead Pickle Inspector",
                                                         "Demonhead Mobster Kingpin",
                                                         "Death",
                                                         "Higgs Bonehead",
                                                         "Future Pickle Inspector",
                                                         "Past Pickle Inspector",
                                                         "Future-Past Pickle Inspector",
                                                         "Future-Future Pickle Inspector"
                                                     };

        static string[] mlpchars = {
	"Twilight Sparkle",
	"Applejack",
	"Fluttershy",
	"Rarity",
	"Pinkie Pie",
	"Rainbow Dash",
	"Spike",
	"Apple Bloom",
	"Scootaloo",
	"Sweetie Belle",
	"Babs Seed",
	"Princess Celestia",
	"Princess Luna",
	"Princess Cadance",
	"Shining Armor",
	"Prince Blueblood",
	"Granny Smith",
	"Big McIntosh",
	"Braeburn",
	"Mr. Cake",
	"Mrs. Cake",
	"Pound Cake",
	"Pumpkin Cake",
	"Diamond Tiara",
	"Silver Spoon",
	"Twist",
	"Snips",
	"Snails",
	"Pipsqueak",
	"Featherweight",
	"Nightmare Moon",
	"Gilda",
	"Trixie",
	"Diamond Dogs",
	"Discord",
	"Flim",
	"Flam",
	"Garble",
	"Queen Chrysalis",
	"King Sombra",
	"Sunset Shimmer",
	"Hoity Toity",
	"Photo Finish",
	"Sapphire Shores",
	"Fancy Pants",
	"Spitfire",
	"Soarin",
	"Mayor Mare",
	"Nurse Redheart",
	"Cheerilee",
	"Jet Set",
	"Upper Crust",
	"Filthy Rich",
	"Joe",
	"Cloudchaser",
	"Flitter",
	"Thunderlane",
	"Blossomforth",
	"Lightning Dust",
	"Ms. Peachbottom",
	"Ms. Harshwhinny",
	"Flash Sentry",
	"Zecora",
	"Little Strongheart",
	"Chief Thunderhooves",
	"Cranky Doodle Donkey",
	"Matilda",
	"Iron Will",
	"Gustave le Grand",
	"Mulia Mild",
	"Bloomberg",
	"Tom",
	"Smarty Pants",
	"Daring Do",
	"Ahuizotl",
	"Angel",
	"Winona",
	"Opalescence",
	"Gummy",
	"Philomena",
	"Owlowiscious",
	"Tank",
	"Peewee",
	"Amethyst Star",
	"Apple Fritter",
	"Aura",
	"Berry Pinch",
	"Berry Punch",
	"Caramel",
	"Cherry Berry",
	"Cloud Kicker",
	"Cotton Cloudy",
	"Daisy",
	"Derpy",
	"Dinky Doo",
	"Dizzy Twister",
	"DJ Pon-3",
	"Dr. Hooves",
	"Golden Harvest",
	"Goldengrape",
	"Mr. Greenhooves",
	"Lemon Hearts",
	"Lightning Bolt",
	"Lily Valley",
	"Lucky Clover",
	"Lyra Heartstrings",
	"Lyrica Lilac",
	"Meadow Song",
	"Medley",
	"Merry May",
	"Minuette",
	"Noi",
	"Noteworthy",
	"Octavia Melody",
	"Parasol",
	"Piña Colada",
	"Princess Erroria",
	"Rainbowshine",
	"Raindrops",
	"Rose",
	"Royal Ribbon",
	"Sassaflash",
	"Sea Swirl",
	"Shoeshine",
	"Snowflake",
	"Sweetie Drops",
	"Tootsie Flute",
	"Tornado Bolt",
	"Twinkleshine",
	"Wild Fire"
};

        static readonly string[] sonicchars = {
	"**********",
	"Alf-Layla-wa-Layla",
	"Ali Baba",
	"Amy Rose",
	"Angelus the Gatekeeper",
	"Ashura",
	"Bark the Polar Bear",
	"Battle Kukku 16th",
	"Bean the Dynamite",
	"Bearenger",
	"Big the Cat",
	"Biolizard",
	"Black Arms",
	"Black Doom",
	"Blacksmith",
	"Blaze the Cat",
	"Blue Knuckles",
	"Bomb",
	"Burning Blaze",
	"Caliburn",
	"Captain Whisker",
	"Carrotia",
	"Chao",
	"Chaos",
	"Chaos Gamma",
	"Chaotix",
	"Charmy Bee",
	"Cheese the Chao",
	"Chip",
	"Coconut Crew",
	"Cream the Rabbit",
	"Cubot",
	"Dark Gaia",
	"Dark Super Sonic",
	"Darkspine Sonic",
	"Doctor Fukurokov",
	"Don Fachio",
	"Dr. Eggman",
	"Duke of Soleanna",
	"E-100 Series",
	"E-10000G",
	"E-10000R",
	"E-101 β",
	"E-101mkII",
	"E-102 γ",
	"E-103 δ",
	"E-104 ε",
	"E-105 ζ",
	"E-121 Phi",
	"E-123 Ω",
	"Eggman Nega",
	"Eggrobo",
	"Princess Elise the Third",
	"Emerl",
	"Erazor Djinn",
	"Espio the Chameleon",
	"Excalibur Sonic",
	"Fang the Sniper",
	"Focke-Wulf",
	"Froggy",
	"G.U.N. Commander",
	"Gaia Colossus",
	"Gemerl",
	"Gerald Robotnik",
	"Grand Battle Kukku 15th",
	"Has Bean",
	"Heavy",
	"Hyper Knuckles",
	"Hyper Sonic",
	"Iblis",
	"Ifrit",
	"Ifrit Golem",
	"Illumina",
	"Ix",
	"Jet the Hawk",
	"Johnny",
	"King Arthur",
	"King Boom Boo",
	"King Shahryār",
	"Knuckles the Echidna",
	"Lily",
	"Lumina Flowlight",
	"Maria Robotnik",
	"Marine the Raccoon",
	"Master Core: ABIS",
	"Mecha Knuckles",
	"Mecha Sonic",
	"Mephiles the Dark",
	"Merlin",
	"Merlina the Wizard",
	"Metal Knuckles",
	"Metal Madness",
	"Metal Overlord",
	"Metal Sonic",
	"Metal Sonic 3.0",
	"Metal Sonic Kai",
	"Mighty the Armadillo",
	"Miles \"Tails\" Prower",
	"Neo Metal Sonic",
	"NiGHTS",
	"Nimue, Lady of the Lake",
	"Omochao",
	"Orbot",
	"Pachacamac",
	"Perfect Chaos",
	"President of the United Federation",
	"Professor Pickle",
	"Ray the Flying Squirrel",
	"Remote Robot",
	"Rocket Metal",
	"Rouge the Bat",
	"SCR-GP",
	"SCR-HD",
	"Shade",
	"Shadow Android",
	"Shadow the Hedgehog",
	"Shahra, the Genie of the Ring",
	"Silver the Hedgehog",
	"Sinbad",
	"Sir Galahad",
	"Sir Gawain",
	"Sir Lamorak",
	"Sir Lancelot",
	"Sir Percival",
	"Solaris",
	"Sonic the Hedgehog",
	"Sonic the Werehog",
	"Storm the Albatross",
	"Super Knuckles",
	"Super Mecha Sonic",
	"Super Shadow",
	"Super Silver",
	"Super Sonic",
	"Super Tails",
	"Tails Doll",
	"Tiara Boobowski",
	"Tikal",
	"Vanilla the Rabbit",
	"Vector the Crocodile",
	"Void",
	"Wave the Swallow",
	"Wentos",
	"Witchcart",
	"Yacker",
	"ZERO"
};

        void SburbgenCommand(IRC IrcObject, string channel, string user, string command)
        {
            Random r = Module1.Random;
            string colorTag = Module1.ColorChar + r.Next(16).ToString().PadLeft(2, '0');

            string title_1 = TitleName1[r.Next(TitleName1.Length)];
            string title_2 = TitleName2[r.Next(TitleName2.Length)];
            string land_1 = LandName[r.Next(LandName.Length)];
            string land_2 = title_2 == "Space" ? "Frogs" : LandName[r.Next(LandName.Length)];
            string chum_1 = Chum1[r.Next(Chum1.Length)];
            string chum_2 = Chum2[r.Next(Chum2.Length)];
            string interest_1 = Interests[r.Next(Interests.Length)];
            string interest_2 = Interests[r.Next(Interests.Length)];
            string specibus = Specibi[r.Next(Specibi.Length)];
            string item_1 = InterestItems[interest_1][r.Next(InterestItems[interest_1].Length)];
            string item_2 = InterestItems[interest_2][r.Next(InterestItems[interest_2].Length)];
            string weapon = SpecibiWeapons[specibus][r.Next(SpecibiWeapons[specibus].Length)];
            string consort_quirk = ConsortQuirks[r.Next(ConsortQuirks.Length)];
            string consort_color = ConsortColors[r.Next(ConsortColors.Length)];
            string consort_type = ConsortTypes[r.Next(ConsortTypes.Length)];
            string consort_like = ConsortLikes[r.Next(ConsortLikes.Length)];

            string bbcode = "You are the " + colorTag;
            bbcode += title_1 + " of " + title_2;
            bbcode += Module1.ColorChar + " in the " + colorTag + "Land of " + land_1;
            bbcode += " and " + land_2;
            bbcode += Module1.ColorChar + ". Your chumHandle is " + colorTag;
            bbcode += chum_1 + chum_2;
            bbcode += Module1.ColorChar + ". ";

            bbcode += "Your interests include " + interest_1.ToUpperInvariant();
            bbcode += " and " + interest_2.ToUpperInvariant() + ". ";
            bbcode += "You wield the " + specibus;
            bbcode += " specibus and have combined your " + weapon.ToUpperInvariant();
            bbcode += " with your " + item_1.ToUpperInvariant();
            bbcode += " and " + item_2.ToUpperInvariant();
            bbcode += " to create your awesome weapon. ";

            bbcode += "The consorts of your land are " + consort_quirk.ToUpperInvariant();
            bbcode += " " + consort_color.ToUpperInvariant();
            bbcode += " " + consort_type.ToUpperInvariant();
            bbcode += " who like " + consort_like.ToUpperInvariant();
            bbcode += ".";
            IrcObject.WriteMessage(user + ": ", bbcode, channel);
        }

        void ChumhandleCommand(IRC IrcObject, string channel, string user, string command)
        {
            Random r = Module1.Random;
            string[] chum1 = Chum1;
            string[] chum2 = Chum2;
            if (command.Equals("/canon", StringComparison.OrdinalIgnoreCase))
            {
                chum1 = Chum1Canon;
                chum2 = Chum2Canon;
            }
            IrcObject.WriteMessage(user + ": ", "Your chumHandle is " + chum1[r.Next(chum1.Length)] + chum2[r.Next(chum2.Length)] + ".", channel);
        }

        void ShipCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] args = Module1.ParseCommandLine(command);
            List<string> chanpeople = new List<string>();
            foreach (IRCUser person in IrcObject.GetChannel(channel, true, user).People)
                chanpeople.Add(person.name);
            List<string> people = new List<string>();
            int ship = -1;
            if (args.Length > 0)
                foreach (string item in args)
                    switch (item.ToLowerInvariant())
                    {
                        case "/hearts":
                            ship = 0;
                            break;
                        case "/diamonds":
                            ship = 1;
                            break;
                        case "/spades":
                            ship = 2;
                            break;
                        case "/clubs":
                            ship = 3;
                            break;
                        case "/hs":
                            people.AddRange(HomestuckCharacters);
                            break;
                        case "/ps":
                            people.AddRange(ProblemSleuthCharacters);
                            break;
                        case "/mlp":
                            people.AddRange(mlpchars);
                            break;
                        case "/sonic":
                            people.AddRange(sonicchars);
                            break;
                        case "/users":
                            people.AddRange(chanpeople);
                            break;
                        case "eridan":
                        case "ampora":
                        case "eridan ampora":
                        case "cronus":
                        case "cronus ampora":
                            throw new ArgumentException();
                        default:
                            people.Add(item);
                            break;
                    }
            if (people.Count == 0)
                people.AddRange(chanpeople);
            if (ship == -1)
                ship = Module1.Random.Next(4);
            string left, right;
            switch (ship)
            {
                case 3:
                    string top;
                    if (people.Count == 1)
                    {
                        top = people[0];
                        left = chanpeople[Module1.Random.Next(chanpeople.Count)];
                        right = chanpeople[Module1.Random.Next(chanpeople.Count)];
                    }
                    else if (people.Count == 2)
                    {
                        top = people[0];
                        left = people[1];
                        right = chanpeople[Module1.Random.Next(chanpeople.Count)];
                    }
                    else if (people.Count == 3)
                    {
                        top = people[0];
                        left = people[1];
                        right = people[2];
                    }
                    else
                    {
                        top = people[Module1.Random.Next(people.Count)];
                        left = people[Module1.Random.Next(people.Count)];
                        right = people[Module1.Random.Next(people.Count)];
                    }
                    IrcObject.WriteMessage(user + ": ", top + " " + Module1.ColorChar + "01\u2663" + Module1.ColorChar + " " + left + " " + Module1.ColorChar + "01\u2660" + Module1.ColorChar + " " + right, channel);
                    break;
                default:
                    if (people.Count == 1)
                    {
                        left = people[0];
                        right = chanpeople[Module1.Random.Next(chanpeople.Count)];
                    }
                    else if (people.Count == 2)
                    {
                        left = people[0];
                        right = people[1];
                    }
                    else
                    {
                        left = people[Module1.Random.Next(people.Count)];
                        right = people[Module1.Random.Next(people.Count)];
                    }
                    IrcObject.WriteMessage(user + ": ", left + " " + Module1.Choose(ship + 1, Module1.ColorChar + "04\u2665" + Module1.ColorChar, Module1.ColorChar + "04\u2666" + Module1.ColorChar, Module1.ColorChar + "01\u2660" + Module1.ColorChar) + " " + right, channel);
                    break;
            }
        }

        static readonly Dictionary<string, string> mspastories = new Dictionary<string, string>() { { "1", "Jailbreak" }, { "2", "Bard Quest" }, { "3", "??????" }, { "4", "Problem Sleuth" }, { "5", "Homestuck BETA" }, { "6", "Homestuck" }, { "ryanquest", "Ryanquest" } };
        static readonly Regex mspapage = new Regex(@"http://.*mspaintadventures\.com/[^\?]*\?s\=([^&]+)\&p\=([^&]+)");
        static readonly Regex mspastory = new Regex(@"http://.*mspaintadventures\.com/[^\?]*\?s\=([^&]+)");
        bool LinkHandler(LinkCheckParams param)
        {
            if (!mspapage.IsMatch(param.Url) && !mspastory.IsMatch(param.Url))
                return false;
            IRC IrcObject = param.IrcObject;
            string Channel = param.Channel;
            string url = param.Url;
            bool fullinfo = param.FullInfo;
            try
            {
                HttpWebRequest s = (HttpWebRequest)HttpWebRequest.Create(url);
                s.Credentials = CredentialCache.DefaultCredentials;
                s.UserAgent = Module1.useragent;
                s.Timeout = 10000;
                s.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                HttpWebResponse response = (HttpWebResponse)s.GetResponse();
                string msg = "Type: " + response.ContentType;
                if (response.ContentLength >= 0)
                    msg += " Size: " + Module1.smartsize((ulong)response.ContentLength);
                TimeSpan updated = DateTime.Now - response.LastModified;
                if (updated > TimeSpan.Zero)
                    msg += " Updated: " + updated.ToStringCust() + " ago";
                if (response.ContentType.StartsWith("text") | response.ContentType.StartsWith("application/xhtml+xml"))
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    reader.Close();
                    if (response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) | response.ContentType.StartsWith("application/xhtml+xml"))
                    {
                        int start = responseFromServer.IndexOf("<title", StringComparison.CurrentCultureIgnoreCase);
                        if (start > -1)
                            start = responseFromServer.IndexOf('>', start) + 1;
                        int length = responseFromServer.IndexOf("</title>", StringComparison.CurrentCultureIgnoreCase) - start;
                        if (start == -1 || length < 0)
                            msg += " Sample: " + responseFromServer.Substring(0, Math.Min(50, responseFromServer.Length)).Replace("\r", " ").Replace("\n", " ") + "...";
                        else
                        {
                            if (fullinfo)
                                msg += " Title: " + System.Web.HttpUtility.HtmlDecode(responseFromServer.Substring(start, length).Replace("\r", " ").Replace("\n", " ")).TrimExcessSpaces().Trim();
                            else
                                msg = "Title: " + System.Web.HttpUtility.HtmlDecode(responseFromServer.Substring(start, length).Replace("\r", " ").Replace("\n", " ")).TrimExcessSpaces().Trim();
                        }
                    }
                    else
                        msg += " Sample: " + responseFromServer.Substring(0, Math.Min(50, responseFromServer.Length)).Replace("\r", " ").Replace("\n", " ") + "...";
                }
                if (mspapage.IsMatch(url))
                {
                    Match match = mspapage.Match(url);
                    msg += ": " + mspastories.GetValueOrDefault(match.Groups[1].Value, "Unknown adventure");
                    string page = match.Groups[2].Value;
                    int p;
                    if (int.TryParse(page, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out p))
                        page = p.ToString("000000");
                    try
                    {
                        s = (HttpWebRequest)HttpWebRequest.Create("http://www.mspaintadventures.com/" + match.Groups[1].Value + "/" + page + ".txt");
                        s.Credentials = CredentialCache.DefaultCredentials;
                        s.UserAgent = Module1.useragent;
                        s.Timeout = 10000;
                        s.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                        response = (HttpWebResponse)s.GetResponse();
                        Stream dataStream = response.GetResponseStream();
                        StreamReader reader = new StreamReader(dataStream);
                        msg += ": " + System.Web.HttpUtility.HtmlDecode(reader.ReadLine());
                        reader.Close();
                    }
                    catch
                    {
                        msg += ": Unknown page";
                    }
                }
                else if (mspastory.IsMatch(url))
                {
                    Match match = mspastory.Match(url);
                    msg += ": " + mspastories.GetValueOrDefault(match.Groups[1].Value, "Unknown adventure");
                }
                IrcObject.WriteMessage(msg, Channel);
            }
            catch (UriFormatException) { }
            catch (Exception ex) { IrcObject.WriteMessage("Link error: " + ex.Message, Channel); }
            return true;
        }
    }
}