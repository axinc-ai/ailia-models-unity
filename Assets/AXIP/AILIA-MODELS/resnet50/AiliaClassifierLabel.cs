﻿/* Imagenet Category List */

using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace ailiaSDK
{
    public class AiliaClassifierLabel
    {

        /* from https://gist.github.com/yrevar/942d3a0ac09ec9e5eb3a */

        public static string[] IMAGENET_CATEGORY = {
 "tench, Tinca tinca",
 "goldfish, Carassius auratus",
 "great white shark, white shark, man-eater, man-eating shark, Carcharodon carcharias",
 "tiger shark, Galeocerdo cuvieri",
 "hammerhead, hammerhead shark",
 "electric ray, crampfish, numbfish, torpedo",
 "stingray",
 "cock",
 "hen",
 "ostrich, Struthio camelus",
 "brambling, Fringilla montifringilla",
 "goldfinch, Carduelis carduelis",
 "house finch, linnet, Carpodacus mexicanus",
 "junco, snowbird",
 "indigo bunting, indigo finch, indigo bird, Passerina cyanea",
 "robin, American robin, Turdus migratorius",
 "bulbul",
 "jay",
 "magpie",
 "chickadee",
 "water ouzel, dipper",
 "kite",
 "bald eagle, American eagle, Haliaeetus leucocephalus",
 "vulture",
 "great grey owl, great gray owl, Strix nebulosa",
 "European fire salamander, Salamandra salamandra",
 "common newt, Triturus vulgaris",
 "eft",
 "spotted salamander, Ambystoma maculatum",
 "axolotl, mud puppy, Ambystoma mexicanum",
 "bullfrog, Rana catesbeiana",
 "tree frog, tree-frog",
 "tailed frog, bell toad, ribbed toad, tailed toad, Ascaphus trui",
 "loggerhead, loggerhead turtle, Caretta caretta",
 "leatherback turtle, leatherback, leathery turtle, Dermochelys coriacea",
 "mud turtle",
 "terrapin",
 "box turtle, box tortoise",
 "banded gecko",
 "common iguana, iguana, Iguana iguana",
 "American chameleon, anole, Anolis carolinensis",
 "whiptail, whiptail lizard",
 "agama",
 "frilled lizard, Chlamydosaurus kingi",
 "alligator lizard",
 "Gila monster, Heloderma suspectum",
 "green lizard, Lacerta viridis",
 "African chameleon, Chamaeleo chamaeleon",
 "Komodo dragon, Komodo lizard, dragon lizard, giant lizard, Varanus komodoensis",
 "African crocodile, Nile crocodile, Crocodylus niloticus",
 "American alligator, Alligator mississipiensis",
 "triceratops",
 "thunder snake, worm snake, Carphophis amoenus",
 "ringneck snake, ring-necked snake, ring snake",
 "hognose snake, puff adder, sand viper",
 "green snake, grass snake",
 "king snake, kingsnake",
 "garter snake, grass snake",
 "water snake",
 "vine snake",
 "night snake, Hypsiglena torquata",
 "boa constrictor, Constrictor constrictor",
 "rock python, rock snake, Python sebae",
 "Indian cobra, Naja naja",
 "green mamba",
 "sea snake",
 "horned viper, cerastes, sand viper, horned asp, Cerastes cornutus",
 "diamondback, diamondback rattlesnake, Crotalus adamanteus",
 "sidewinder, horned rattlesnake, Crotalus cerastes",
 "trilobite",
 "harvestman, daddy longlegs, Phalangium opilio",
 "scorpion",
 "black and gold garden spider, Argiope aurantia",
 "barn spider, Araneus cavaticus",
 "garden spider, Aranea diademata",
 "black widow, Latrodectus mactans",
 "tarantula",
 "wolf spider, hunting spider",
 "tick",
 "centipede",
 "black grouse",
 "ptarmigan",
 "ruffed grouse, partridge, Bonasa umbellus",
 "prairie chicken, prairie grouse, prairie fowl",
 "peacock",
 "quail",
 "partridge",
 "African grey, African gray, Psittacus erithacus",
 "macaw",
 "sulphur-crested cockatoo, Kakatoe galerita, Cacatua galerita",
 "lorikeet",
 "coucal",
 "bee eater",
 "hornbill",
 "hummingbird",
 "jacamar",
 "toucan",
 "drake",
 "red-breasted merganser, Mergus serrator",
 "goose",
 "black swan, Cygnus atratus",
 "tusker",
 "echidna, spiny anteater, anteater",
 "platypus, duckbill, duckbilled platypus, duck-billed platypus, Ornithorhynchus anatinus",
 "wallaby, brush kangaroo",
 "koala, koala bear, kangaroo bear, native bear, Phascolarctos cinereus",
 "wombat",
 "jellyfish",
 "sea anemone, anemone",
 "brain coral",
 "flatworm, platyhelminth",
 "nematode, nematode worm, roundworm",
 "conch",
 "snail",
 "slug",
 "sea slug, nudibranch",
 "chiton, coat-of-mail shell, sea cradle, polyplacophore",
 "chambered nautilus, pearly nautilus, nautilus",
 "Dungeness crab, Cancer magister",
 "rock crab, Cancer irroratus",
 "fiddler crab",
 "king crab, Alaska crab, Alaskan king crab, Alaska king crab, Paralithodes camtschatica",
 "American lobster, Northern lobster, Maine lobster, Homarus americanus",
 "spiny lobster, langouste, rock lobster, crawfish, crayfish, sea crawfish",
 "crayfish, crawfish, crawdad, crawdaddy",
 "hermit crab",
 "isopod",
 "white stork, Ciconia ciconia",
 "black stork, Ciconia nigra",
 "spoonbill",
 "flamingo",
 "little blue heron, Egretta caerulea",
 "American egret, great white heron, Egretta albus",
 "bittern",
 "crane",
 "limpkin, Aramus pictus",
 "European gallinule, Porphyrio porphyrio",
 "American coot, marsh hen, mud hen, water hen, Fulica americana",
 "bustard",
 "ruddy turnstone, Arenaria interpres",
 "red-backed sandpiper, dunlin, Erolia alpina",
 "redshank, Tringa totanus",
 "dowitcher",
 "oystercatcher, oyster catcher",
 "pelican",
 "king penguin, Aptenodytes patagonica",
 "albatross, mollymawk",
 "grey whale, gray whale, devilfish, Eschrichtius gibbosus, Eschrichtius robustus",
 "killer whale, killer, orca, grampus, sea wolf, Orcinus orca",
 "dugong, Dugong dugon",
 "sea lion",
 "Chihuahua",
 "Japanese spaniel",
 "Maltese dog, Maltese terrier, Maltese",
 "Pekinese, Pekingese, Peke",
 "shih-Tzu",
 "Blenheim spaniel",
 "papillon",
 "toy terrier",
 "Rhodesian ridgeback",
 "Afghan hound, Afghan",
 "basset, basset hound",
 "beagle",
 "bloodhound, sleuthhound",
 "bluetick",
 "black-and-tan coonhound",
 "Walker hound, Walker foxhound",
 "English foxhound",
 "redbone",
 "borzoi, Russian wolfhound",
 "Irish wolfhound",
 "Italian greyhound",
 "whippet",
 "Ibizan hound, Ibizan Podenco",
 "Norwegian elkhound, elkhound",
 "otterhound, otter hound",
 "saluki, gazelle hound",
 "scottish deerhound, deerhound",
 "Weimaraner",
 "staffordshire bullterrier, Staffordshire bull terrier",
 "American Staffordshire terrier, Staffordshire terrier, American pit bull terrier, pit bull terrier",
 "Bedlington terrier",
 "Border terrier",
 "Kerry blue terrier",
 "Irish terrier",
 "Norfolk terrier",
 "Norwich terrier",
 "Yorkshire terrier",
 "wire-haired fox terrier",
 "Lakeland terrier",
 "sealyham terrier, Sealyham",
 "Airedale, Airedale terrier",
 "cairn, cairn terrier",
 "Australian terrier",
 "Dandie Dinmont, Dandie Dinmont terrier",
 "Boston bull, Boston terrier",
 "miniature schnauzer",
 "giant schnauzer",
 "standard schnauzer",
 "scotch terrier, Scottish terrier, Scottie",
 "Tibetan terrier, chrysanthemum dog",
 "silky terrier, Sydney silky",
 "soft-coated wheaten terrier",
 "West Highland white terrier",
 "Lhasa, Lhasa apso",
 "flat-coated retriever",
 "curly-coated retriever",
 "golden retriever",
 "Labrador retriever",
 "Chesapeake Bay retriever",
 "German short-haired pointer",
 "vizsla, Hungarian pointer",
 "English setter",
 "Irish setter, red setter",
 "Gordon setter",
 "Brittany spaniel",
 "clumber, clumber spaniel",
 "English springer, English springer spaniel",
 "Welsh springer spaniel",
 "cocker spaniel, English cocker spaniel, cocker",
 "sussex spaniel",
 "Irish water spaniel",
 "kuvasz",
 "schipperke",
 "groenendael",
 "malinois",
 "briard",
 "kelpie",
 "komondor",
 "Old English sheepdog, bobtail",
 "shetland sheepdog, Shetland sheep dog, Shetland",
 "collie",
 "Border collie",
 "Bouvier des Flandres, Bouviers des Flandres",
 "Rottweiler",
 "German shepherd, German shepherd dog, German police dog, alsatian",
 "Doberman, Doberman pinscher",
 "miniature pinscher",
 "Greater Swiss Mountain dog",
 "Bernese mountain dog",
 "Appenzeller",
 "EntleBucher",
 "boxer",
 "bull mastiff",
 "Tibetan mastiff",
 "French bulldog",
 "Great Dane",
 "saint Bernard, St Bernard",
 "Eskimo dog, husky",
 "malamute, malemute, Alaskan malamute",
 "siberian husky",
 "dalmatian, coach dog, carriage dog",
 "affenpinscher, monkey pinscher, monkey dog",
 "basenji",
 "pug, pug-dog",
 "Leonberg",
 "Newfoundland, Newfoundland dog",
 "Great Pyrenees",
 "samoyed, Samoyede",
 "Pomeranian",
 "chow, chow chow",
 "keeshond",
 "Brabancon griffon",
 "Pembroke, Pembroke Welsh corgi",
 "Cardigan, Cardigan Welsh corgi",
 "toy poodle",
 "miniature poodle",
 "standard poodle",
 "Mexican hairless",
 "timber wolf, grey wolf, gray wolf, Canis lupus",
 "white wolf, Arctic wolf, Canis lupus tundrarum",
 "red wolf, maned wolf, Canis rufus, Canis niger",
 "coyote, prairie wolf, brush wolf, Canis latrans",
 "dingo, warrigal, warragal, Canis dingo",
 "dhole, Cuon alpinus",
 "African hunting dog, hyena dog, Cape hunting dog, Lycaon pictus",
 "hyena, hyaena",
 "red fox, Vulpes vulpes",
 "kit fox, Vulpes macrotis",
 "Arctic fox, white fox, Alopex lagopus",
 "grey fox, gray fox, Urocyon cinereoargenteus",
 "tabby, tabby cat",
 "tiger cat",
 "Persian cat",
 "siamese cat, Siamese",
 "Egyptian cat",
 "cougar, puma, catamount, mountain lion, painter, panther, Felis concolor",
 "lynx, catamount",
 "leopard, Panthera pardus",
 "snow leopard, ounce, Panthera uncia",
 "jaguar, panther, Panthera onca, Felis onca",
 "lion, king of beasts, Panthera leo",
 "tiger, Panthera tigris",
 "cheetah, chetah, Acinonyx jubatus",
 "brown bear, bruin, Ursus arctos",
 "American black bear, black bear, Ursus americanus, Euarctos americanus",
 "ice bear, polar bear, Ursus Maritimus, Thalarctos maritimus",
 "sloth bear, Melursus ursinus, Ursus ursinus",
 "mongoose",
 "meerkat, mierkat",
 "tiger beetle",
 "ladybug, ladybeetle, lady beetle, ladybird, ladybird beetle",
 "ground beetle, carabid beetle",
 "long-horned beetle, longicorn, longicorn beetle",
 "leaf beetle, chrysomelid",
 "dung beetle",
 "rhinoceros beetle",
 "weevil",
 "fly",
 "bee",
 "ant, emmet, pismire",
 "grasshopper, hopper",
 "cricket",
 "walking stick, walkingstick, stick insect",
 "cockroach, roach",
 "mantis, mantid",
 "cicada, cicala",
 "leafhopper",
 "lacewing, lacewing fly",
 "dragonfly, darning needle, devil's darning needle, sewing needle, snake feeder, snake doctor, mosquito hawk, skeeter hawk",
 "damselfly",
 "admiral",
 "ringlet, ringlet butterfly",
 "monarch, monarch butterfly, milkweed butterfly, Danaus plexippus",
 "cabbage butterfly",
 "sulphur butterfly, sulfur butterfly",
 "lycaenid, lycaenid butterfly",
 "starfish, sea star",
 "sea urchin",
 "sea cucumber, holothurian",
 "wood rabbit, cottontail, cottontail rabbit",
 "hare",
 "Angora, Angora rabbit",
 "hamster",
 "porcupine, hedgehog",
 "fox squirrel, eastern fox squirrel, Sciurus niger",
 "marmot",
 "beaver",
 "guinea pig, Cavia cobaya",
 "sorrel",
 "zebra",
 "hog, pig, grunter, squealer, Sus scrofa",
 "wild boar, boar, Sus scrofa",
 "warthog",
 "hippopotamus, hippo, river horse, Hippopotamus amphibius",
 "ox",
 "water buffalo, water ox, Asiatic buffalo, Bubalus bubalis",
 "bison",
 "ram, tup",
 "bighorn, bighorn sheep, cimarron, Rocky Mountain bighorn, Rocky Mountain sheep, Ovis canadensis",
 "ibex, Capra ibex",
 "hartebeest",
 "impala, Aepyceros melampus",
 "gazelle",
 "Arabian camel, dromedary, Camelus dromedarius",
 "llama",
 "weasel",
 "mink",
 "polecat, fitch, foulmart, foumart, Mustela putorius",
 "black-footed ferret, ferret, Mustela nigripes",
 "otter",
 "skunk, polecat, wood pussy",
 "badger",
 "armadillo",
 "three-toed sloth, ai, Bradypus tridactylus",
 "orangutan, orang, orangutang, Pongo pygmaeus",
 "gorilla, Gorilla gorilla",
 "chimpanzee, chimp, Pan troglodytes",
 "gibbon, Hylobates lar",
 "siamang, Hylobates syndactylus, Symphalangus syndactylus",
 "guenon, guenon monkey",
 "patas, hussar monkey, Erythrocebus patas",
 "baboon",
 "macaque",
 "langur",
 "colobus, colobus monkey",
 "proboscis monkey, Nasalis larvatus",
 "marmoset",
 "capuchin, ringtail, Cebus capucinus",
 "howler monkey, howler",
 "titi, titi monkey",
 "spider monkey, Ateles geoffroyi",
 "squirrel monkey, Saimiri sciureus",
 "Madagascar cat, ring-tailed lemur, Lemur catta",
 "indri, indris, Indri indri, Indri brevicaudatus",
 "Indian elephant, Elephas maximus",
 "African elephant, Loxodonta africana",
 "lesser panda, red panda, panda, bear cat, cat bear, Ailurus fulgens",
 "giant panda, panda, panda bear, coon bear, Ailuropoda melanoleuca",
 "barracouta, snoek",
 "eel",
 "coho, cohoe, coho salmon, blue jack, silver salmon, Oncorhynchus kisutch",
 "rock beauty, Holocanthus tricolor",
 "anemone fish",
 "sturgeon",
 "gar, garfish, garpike, billfish, Lepisosteus osseus",
 "lionfish",
 "puffer, pufferfish, blowfish, globefish",
 "abacus",
 "abaya",
 "academic gown, academic robe, judge's robe",
 "accordion, piano accordion, squeeze box",
 "acoustic guitar",
 "aircraft carrier, carrier, flattop, attack aircraft carrier",
 "airliner",
 "airship, dirigible",
 "altar",
 "ambulance",
 "amphibian, amphibious vehicle",
 "analog clock",
 "apiary, bee house",
 "apron",
 "ashcan, trash can, garbage can, wastebin, ash bin, ash-bin, ashbin, dustbin, trash barrel, trash bin",
 "assault rifle, assault gun",
 "backpack, back pack, knapsack, packsack, rucksack, haversack",
 "bakery, bakeshop, bakehouse",
 "balance beam, beam",
 "balloon",
 "ballpoint, ballpoint pen, ballpen, Biro",
 "Band Aid",
 "banjo",
 "bannister, banister, balustrade, balusters, handrail",
 "barbell",
 "barber chair",
 "barbershop",
 "barn",
 "barometer",
 "barrel, cask",
 "barrow, garden cart, lawn cart, wheelbarrow",
 "baseball",
 "basketball",
 "bassinet",
 "bassoon",
 "bathing cap, swimming cap",
 "bath towel",
 "bathtub, bathing tub, bath, tub",
 "beach wagon, station wagon, wagon, estate car, beach waggon, station waggon, waggon",
 "beacon, lighthouse, beacon light, pharos",
 "beaker",
 "bearskin, busby, shako",
 "beer bottle",
 "beer glass",
 "bell cote, bell cot",
 "bib",
 "bicycle-built-for-two, tandem bicycle, tandem",
 "bikini, two-piece",
 "binder, ring-binder",
 "binoculars, field glasses, opera glasses",
 "birdhouse",
 "boathouse",
 "bobsled, bobsleigh, bob",
 "bolo tie, bolo, bola tie, bola",
 "bonnet, poke bonnet",
 "bookcase",
 "bookshop, bookstore, bookstall",
 "bottlecap",
 "bow",
 "bow tie, bow-tie, bowtie",
 "brass, memorial tablet, plaque",
 "brassiere, bra, bandeau",
 "breakwater, groin, groyne, mole, bulwark, seawall, jetty",
 "breastplate, aegis, egis",
 "broom",
 "bucket, pail",
 "buckle",
 "bulletproof vest",
 "bullet train, bullet",
 "butcher shop, meat market",
 "cab, hack, taxi, taxicab",
 "caldron, cauldron",
 "candle, taper, wax light",
 "cannon",
 "canoe",
 "can opener, tin opener",
 "cardigan",
 "car mirror",
 "carousel, carrousel, merry-go-round, roundabout, whirligig",
 "carpenter's kit, tool kit",
 "carton",
 "car wheel",
 "cash machine, cash dispenser, automated teller machine, automatic teller machine, automated teller, automatic teller, ATM",
 "cassette",
 "cassette player",
 "castle",
 "catamaran",
 "CD player",
 "cello, violoncello",
 "cellular telephone, cellular phone, cellphone, cell, mobile phone",
 "chain",
 "chainlink fence",
 "chain mail, ring mail, mail, chain armor, chain armour, ring armor, ring armour",
 "chain saw, chainsaw",
 "chest",
 "chiffonier, commode",
 "chime, bell, gong",
 "china cabinet, china closet",
 "Christmas stocking",
 "church, church building",
 "cinema, movie theater, movie theatre, movie house, picture palace",
 "cleaver, meat cleaver, chopper",
 "cliff dwelling",
 "cloak",
 "clog, geta, patten, sabot",
 "cocktail shaker",
 "coffee mug",
 "coffeepot",
 "coil, spiral, volute, whorl, helix",
 "combination lock",
 "computer keyboard, keypad",
 "confectionery, confectionary, candy store",
 "container ship, containership, container vessel",
 "convertible",
 "corkscrew, bottle screw",
 "cornet, horn, trumpet, trump",
 "cowboy boot",
 "cowboy hat, ten-gallon hat",
 "cradle",
 "crane",
 "crash helmet",
 "crate",
 "crib, cot",
 "Crock Pot",
 "croquet ball",
 "crutch",
 "cuirass",
 "dam, dike, dyke",
 "desk",
 "desktop computer",
 "dial telephone, dial phone",
 "diaper, nappy, napkin",
 "digital clock",
 "digital watch",
 "dining table, board",
 "dishrag, dishcloth",
 "dishwasher, dish washer, dishwashing machine",
 "disk brake, disc brake",
 "dock, dockage, docking facility",
 "dogsled, dog sled, dog sleigh",
 "dome",
 "doormat, welcome mat",
 "drilling platform, offshore rig",
 "drum, membranophone, tympan",
 "drumstick",
 "dumbbell",
 "Dutch oven",
 "electric fan, blower",
 "electric guitar",
 "electric locomotive",
 "entertainment center",
 "envelope",
 "espresso maker",
 "face powder",
 "feather boa, boa",
 "file, file cabinet, filing cabinet",
 "fireboat",
 "fire engine, fire truck",
 "fire screen, fireguard",
 "flagpole, flagstaff",
 "flute, transverse flute",
 "folding chair",
 "football helmet",
 "forklift",
 "fountain",
 "fountain pen",
 "four-poster",
 "freight car",
 "French horn, horn",
 "frying pan, frypan, skillet",
 "fur coat",
 "garbage truck, dustcart",
 "gasmask, respirator, gas helmet",
 "gas pump, gasoline pump, petrol pump, island dispenser",
 "goblet",
 "go-kart",
 "golf ball",
 "golfcart, golf cart",
 "gondola",
 "gong, tam-tam",
 "gown",
 "grand piano, grand",
 "greenhouse, nursery, glasshouse",
 "grille, radiator grille",
 "grocery store, grocery, food market, market",
 "guillotine",
 "hair slide",
 "hair spray",
 "half track",
 "hammer",
 "hamper",
 "hand blower, blow dryer, blow drier, hair dryer, hair drier",
 "hand-held computer, hand-held microcomputer",
 "handkerchief, hankie, hanky, hankey",
 "hard disc, hard disk, fixed disk",
 "harmonica, mouth organ, harp, mouth harp",
 "harp",
 "harvester, reaper",
 "hatchet",
 "holster",
 "home theater, home theatre",
 "honeycomb",
 "hook, claw",
 "hoopskirt, crinoline",
 "horizontal bar, high bar",
 "horse cart, horse-cart",
 "hourglass",
 "iPod",
 "iron, smoothing iron",
 "jack-o'-lantern",
 "jean, blue jean, denim",
 "jeep, landrover",
 "jersey, T-shirt, tee shirt",
 "jigsaw puzzle",
 "jinrikisha, ricksha, rickshaw",
 "joystick",
 "kimono",
 "knee pad",
 "knot",
 "lab coat, laboratory coat",
 "ladle",
 "lampshade, lamp shade",
 "laptop, laptop computer",
 "lawn mower, mower",
 "lens cap, lens cover",
 "letter opener, paper knife, paperknife",
 "library",
 "lifeboat",
 "lighter, light, igniter, ignitor",
 "limousine, limo",
 "liner, ocean liner",
 "lipstick, lip rouge",
 "Loafer",
 "lotion",
 "loudspeaker, speaker, speaker unit, loudspeaker system, speaker system",
 "loupe, jeweler's loupe",
 "lumbermill, sawmill",
 "magnetic compass",
 "mailbag, postbag",
 "mailbox, letter box",
 "maillot",
 "maillot, tank suit",
 "manhole cover",
 "maraca",
 "marimba, xylophone",
 "mask",
 "matchstick",
 "maypole",
 "maze, labyrinth",
 "measuring cup",
 "medicine chest, medicine cabinet",
 "megalith, megalithic structure",
 "microphone, mike",
 "microwave, microwave oven",
 "military uniform",
 "milk can",
 "minibus",
 "miniskirt, mini",
 "minivan",
 "missile",
 "mitten",
 "mixing bowl",
 "mobile home, manufactured home",
 "Model T",
 "modem",
 "monastery",
 "monitor",
 "moped",
 "mortar",
 "mortarboard",
 "mosque",
 "mosquito net",
 "motor scooter, scooter",
 "mountain bike, all-terrain bike, off-roader",
 "mountain tent",
 "mouse, computer mouse",
 "mousetrap",
 "moving van",
 "muzzle",
 "nail",
 "neck brace",
 "necklace",
 "nipple",
 "notebook, notebook computer",
 "obelisk",
 "oboe, hautboy, hautbois",
 "ocarina, sweet potato",
 "odometer, hodometer, mileometer, milometer",
 "oil filter",
 "organ, pipe organ",
 "oscilloscope, scope, cathode-ray oscilloscope, CRO",
 "overskirt",
 "oxcart",
 "oxygen mask",
 "packet",
 "paddle, boat paddle",
 "paddlewheel, paddle wheel",
 "padlock",
 "paintbrush",
 "pajama, pyjama, pj's, jammies",
 "palace",
 "panpipe, pandean pipe, syrinx",
 "paper towel",
 "parachute, chute",
 "parallel bars, bars",
 "park bench",
 "parking meter",
 "passenger car, coach, carriage",
 "patio, terrace",
 "pay-phone, pay-station",
 "pedestal, plinth, footstall",
 "pencil box, pencil case",
 "pencil sharpener",
 "perfume, essence",
 "Petri dish",
 "photocopier",
 "pick, plectrum, plectron",
 "pickelhaube",
 "picket fence, paling",
 "pickup, pickup truck",
 "pier",
 "piggy bank, penny bank",
 "pill bottle",
 "pillow",
 "ping-pong ball",
 "pinwheel",
 "pirate, pirate ship",
 "pitcher, ewer",
 "plane, carpenter's plane, woodworking plane",
 "planetarium",
 "plastic bag",
 "plate rack",
 "plow, plough",
 "plunger, plumber's helper",
 "Polaroid camera, Polaroid Land camera",
 "pole",
 "police van, police wagon, paddy wagon, patrol wagon, wagon, black Maria",
 "poncho",
 "pool table, billiard table, snooker table",
 "pop bottle, soda bottle",
 "pot, flowerpot",
 "potter's wheel",
 "power drill",
 "prayer rug, prayer mat",
 "printer",
 "prison, prison house",
 "projectile, missile",
 "projector",
 "puck, hockey puck",
 "punching bag, punch bag, punching ball, punchball",
 "purse",
 "quill, quill pen",
 "quilt, comforter, comfort, puff",
 "racer, race car, racing car",
 "racket, racquet",
 "radiator",
 "radio, wireless",
 "radio telescope, radio reflector",
 "rain barrel",
 "recreational vehicle, RV, R.V.",
 "reel",
 "reflex camera",
 "refrigerator, icebox",
 "remote control, remote",
 "restaurant, eating house, eating place, eatery",
 "revolver, six-gun, six-shooter",
 "rifle",
 "rocking chair, rocker",
 "rotisserie",
 "rubber eraser, rubber, pencil eraser",
 "rugby ball",
 "rule, ruler",
 "running shoe",
 "safe",
 "safety pin",
 "saltshaker, salt shaker",
 "sandal",
 "sarong",
 "sax, saxophone",
 "scabbard",
 "scale, weighing machine",
 "school bus",
 "schooner",
 "scoreboard",
 "screen, CRT screen",
 "screw",
 "screwdriver",
 "seat belt, seatbelt",
 "sewing machine",
 "shield, buckler",
 "shoe shop, shoe-shop, shoe store",
 "shoji",
 "shopping basket",
 "shopping cart",
 "shovel",
 "shower cap",
 "shower curtain",
 "ski",
 "ski mask",
 "sleeping bag",
 "slide rule, slipstick",
 "sliding door",
 "slot, one-armed bandit",
 "snorkel",
 "snowmobile",
 "snowplow, snowplough",
 "soap dispenser",
 "soccer ball",
 "sock",
 "solar dish, solar collector, solar furnace",
 "sombrero",
 "soup bowl",
 "space bar",
 "space heater",
 "space shuttle",
 "spatula",
 "speedboat",
 "spider web, spider's web",
 "spindle",
 "sports car, sport car",
 "spotlight, spot",
 "stage",
 "steam locomotive",
 "steel arch bridge",
 "steel drum",
 "stethoscope",
 "stole",
 "stone wall",
 "stopwatch, stop watch",
 "stove",
 "strainer",
 "streetcar, tram, tramcar, trolley, trolley car",
 "stretcher",
 "studio couch, day bed",
 "stupa, tope",
 "submarine, pigboat, sub, U-boat",
 "suit, suit of clothes",
 "sundial",
 "sunglass",
 "sunglasses, dark glasses, shades",
 "sunscreen, sunblock, sun blocker",
 "suspension bridge",
 "swab, swob, mop",
 "sweatshirt",
 "swimming trunks, bathing trunks",
 "swing",
 "switch, electric switch, electrical switch",
 "syringe",
 "table lamp",
 "tank, army tank, armored combat vehicle, armoured combat vehicle",
 "tape player",
 "teapot",
 "teddy, teddy bear",
 "television, television system",
 "tennis ball",
 "thatch, thatched roof",
 "theater curtain, theatre curtain",
 "thimble",
 "thresher, thrasher, threshing machine",
 "throne",
 "tile roof",
 "toaster",
 "tobacco shop, tobacconist shop, tobacconist",
 "toilet seat",
 "torch",
 "totem pole",
 "tow truck, tow car, wrecker",
 "toyshop",
 "tractor",
 "trailer truck, tractor trailer, trucking rig, rig, articulated lorry, semi",
 "tray",
 "trench coat",
 "tricycle, trike, velocipede",
 "trimaran",
 "tripod",
 "triumphal arch",
 "trolleybus, trolley coach, trackless trolley",
 "trombone",
 "tub, vat",
 "turnstile",
 "typewriter keyboard",
 "umbrella",
 "unicycle, monocycle",
 "upright, upright piano",
 "vacuum, vacuum cleaner",
 "vase",
 "vault",
 "velvet",
 "vending machine",
 "vestment",
 "viaduct",
 "violin, fiddle",
 "volleyball",
 "waffle iron",
 "wall clock",
 "wallet, billfold, notecase, pocketbook",
 "wardrobe, closet, press",
 "warplane, military plane",
 "washbasin, handbasin, washbowl, lavabo, wash-hand basin",
 "washer, automatic washer, washing machine",
 "water bottle",
 "water jug",
 "water tower",
 "whiskey jug",
 "whistle",
 "wig",
 "window screen",
 "window shade",
 "Windsor tie",
 "wine bottle",
 "wing",
 "wok",
 "wooden spoon",
 "wool, woolen, woollen",
 "worm fence, snake fence, snake-rail fence, Virginia fence",
 "wreck",
 "yawl",
 "yurt",
 "web site, website, internet site, site",
 "comic book",
 "crossword puzzle, crossword",
 "street sign",
 "traffic light, traffic signal, stoplight",
 "book jacket, dust cover, dust jacket, dust wrapper",
 "menu",
 "plate",
 "guacamole",
 "consomme",
 "hot pot, hotpot",
 "trifle",
 "ice cream, icecream",
 "ice lolly, lolly, lollipop, popsicle",
 "French loaf",
 "bagel, beigel",
 "pretzel",
 "cheeseburger",
 "hotdog, hot dog, red hot",
 "mashed potato",
 "head cabbage",
 "broccoli",
 "cauliflower",
 "zucchini, courgette",
 "spaghetti squash",
 "acorn squash",
 "butternut squash",
 "cucumber, cuke",
 "artichoke, globe artichoke",
 "bell pepper",
 "cardoon",
 "mushroom",
 "Granny Smith",
 "strawberry",
 "orange",
 "lemon",
 "fig",
 "pineapple, ananas",
 "banana",
 "jackfruit, jak, jack",
 "custard apple",
 "pomegranate",
 "hay",
 "carbonara",
 "chocolate sauce, chocolate syrup",
 "dough",
 "meat loaf, meatloaf",
 "pizza, pizza pie",
 "potpie",
 "burrito",
 "red wine",
 "espresso",
 "cup",
 "eggnog",
 "alp",
 "bubble",
 "cliff, drop, drop-off",
 "coral reef",
 "geyser",
 "lakeside, lakeshore",
 "promontory, headland, head, foreland",
 "sandbar, sand bar",
 "seashore, coast, seacoast, sea-coast",
 "valley, vale",
 "volcano",
 "ballplayer, baseball player",
 "groom, bridegroom",
 "scuba diver",
 "rapeseed",
 "daisy",
 "yellow lady's slipper, yellow lady-slipper, Cypripedium calceolus, Cypripedium parviflorum",
 "corn",
 "acorn",
 "hip, rose hip, rosehip",
 "buckeye, horse chestnut, conker",
 "coral fungus",
 "agaric",
 "gyromitra",
 "stinkhorn, carrion fungus",
 "earthstar",
 "hen-of-the-woods, hen of the woods, Polyporus frondosus, Grifola frondosa",
 "bolete",
 "ear, spike, capitulum",
 "toilet tissue, toilet paper, bathroom tissue"};



        /*
        from https://gist.github.com/PonDad/4dcb4b242b9358e524b4ddecbee385e9

        */

        public static string[] IMAGENET_CATEGORY_JP = {
"テンチ",
"金魚",
"ホホジロザメ",
"イタチザメ",
"ハンマーヘッド",
"シビレエイ",
"アカエイ",
"コック",
"めんどり",
"ダチョウ",
"アトリ",
"ゴシキヒワ",
"ハウスフィンチ",
"ユキヒメドリ",
"インディゴホオジロ",
"ロビン",
"ブルブル",
"カケス",
"カササギ",
"四十雀",
"水クロウタドリ",
"凧",
"白頭ワシ",
"ハゲワシ",
"カラフトフクロウ",
"欧州ファイアサラマンダー",
"共通イモリ",
"イモリ",
"サンショウウオを発見",
"アホロートル",
"ウシガエル",
"アマガエル",
"つかれたカエル",
"とんちき",
"オサガメ",
"鼈",
"テラピン",
"ハコガメ",
"縞模様のヤモリ",
"共通イグアナ",
"アメリカンカメレオン",
"ウィッペイル",
"アガマトカゲ",
"フリルトカゲ",
"アリゲータートカゲ",
"アメリカドクトカゲ",
"緑のトカゲ",
"アフリカのカメレオン",
"コモドドラゴン",
"アフリカのワニ",
"アメリカワニ",
"トリケラトプス",
"雷のヘビ",
"リングネックスネーク",
"ホーノースヘビ",
"緑のヘビ",
"キングスネーク",
"ガータースネーク",
"水蛇",
"つるヘビ",
"夜のヘビ",
"ボア・コンストリクター",
"ロックパイソン",
"インドコブラ",
"グリーンマンバ",
"ウミヘビ",
"ツノクサリヘビ",
"ダイヤ",
"サイドワインダー",
"三葉虫",
"刈り入れ作業者",
"サソリ",
"黒と金の庭クモ",
"納屋クモ",
"庭クモ",
"クロゴケグモ",
"タランチュラ",
"オオカミのクモ",
"ダニ",
"百足",
"クロライチョウ",
"雷鳥",
"ひだえりの付いたライチョウ",
"草原チキン",
"孔雀",
"ウズラ",
"ヤマウズラ",
"アフリカの灰色",
"コンゴウインコ",
"硫黄トキオウム",
"インコ",
"バンケン",
"蜂食べる人",
"サイチョウ",
"ハチドリ",
"錐嘴",
"オオハシ",
"ドレイク",
"赤ブレストアイサ属のガモ",
"ガチョウ",
"黒い白鳥",
"タスカービール",
"ハリモグラ",
"カモノハシ",
"ワラビー",
"コアラ",
"ウォンバット",
"クラゲ",
"イソギンチャク",
"脳サンゴ",
"扁形動物",
"線虫",
"巻き貝",
"カタツムリ",
"ナメクジ",
"ウミウシ",
"キトン",
"オウムガイ",
"アメリカイチョウガニ",
"岩カニ",
"シオマネキ",
"タラバガニ",
"アメリカンロブスター",
"伊勢エビ",
"ザリガニ",
"ヤドカリ",
"等脚類",
"コウノトリ",
"ナベコウ",
"ヘラサギ",
"フラミンゴ",
"小さな青いサギ",
"アメリカン白鷺",
"にがり",
"クレーン",
"ツルモドキ科の鳥",
"ヨーロピアン水鳥",
"アメリカオオバン",
"ノガン",
"キョウジョシギ",
"赤担保シギ",
"アカアシシギ",
"オオハシシギ",
"ミヤコドリ",
"ペリカン",
"キングペンギン",
"アルバトロス",
"コククジラ",
"シャチ",
"ジュゴン",
"アシカ",
"チワワ",
"狆",
"マルチーズ犬",
"狆",
"シーズー、シーズー",
"ブレナムスパニエル",
"パピヨン",
"トイテリア",
"ローデシアン・リッジバック",
"アフガンハウンド",
"バセット犬",
"ビーグル",
"ブラッドハウンド",
"ブルーティック",
"黒と黄褐色の猟犬",
"ウォーカーハウンド",
"イングリッシュフォックスハウンド",
"レッドボーン",
"ボルゾイ",
"アイリッシュ・ウルフハウンド",
"イタリアングレーハウンド",
"ウィペット",
"イビサハウンド",
"ノルウェーエルクハウンド",
"オッターハウンド",
"サルーキ",
"スコティッシュ・ディアハウンド",
"ワイマラナー",
"スタフォードシャーブルテリア",
"アメリカン・スタッフォードシャー・テリア",
"ベドリントンテリア",
"ボーダーテリア",
"ケリーブルーテリア",
"アイリッシュテリア",
"ノーフォークテリア",
"ノーリッチ・テリア",
"ヨークシャーテリア",
"ワイヤーヘアー・フォックステリア",
"レークランドテリア",
"シーリーハムテリア",
"エアデール",
"ケルン",
"オーストラリアテリア",
"ダンディディンモントテリア",
"ボストンブル",
"ミニチュアシュナウザー",
"ジャイアントシュナウザー",
"スタンダードシュナウザー",
"スコッチテリア",
"チベタンテリア",
"シルキーテリア",
"ソフトコーテッド・ウィートン・テリア",
"ウェストハイランドホワイトテリア",
"ラサ",
"フラットコーテッド・レトリーバー",
"カーリーコーティングされたレトリーバー",
"ゴールデンレトリバー",
"ラブラドル・レトリーバー犬",
"チェサピーク湾レトリーバー",
"ジャーマン・ショートヘア・ポインタ",
"ビズラ",
"イングリッシュセッター",
"アイリッシュセッター",
"ゴードンセッター",
"ブリタニースパニエル",
"クランバー",
"イングリッシュスプリンガー",
"ウェルシュスプリンガースパニエル",
"コッカースパニエル",
"サセックススパニエル",
"アイルランドのウォータースパニエル",
"クバース犬",
"スキッパーキー",
"ベルジアン・シェパード・ドッグ・グローネンダール",
"マリノア",
"ブリアール",
"ケルピー",
"コモンドール",
"オールドイングリッシュシープドッグ",
"シェトランドシープドッグ",
"コリー",
"ボーダーコリー",
"ブーヴィエ・デ・フランドル",
"ロットワイラー",
"ジャーマンシェパード",
"ドーベルマン犬",
"ミニチュアピンシャー",
"グレータースイスマウンテンドッグ",
"バーネーズマウンテンドッグ",
"アッペンツェル",
"エントレブッシャー",
"ボクサー",
"ブルマスチフ",
"チベットマスチフ",
"フレンチブルドッグ",
"グレートデーン",
"セントバーナード",
"エスキモー犬",
"マラミュート",
"シベリアンハスキー",
"ダルメシアン",
"アーフェンピンシャー",
"バセンジー",
"パグ",
"レオンバーグ",
"ニューファンドランド島",
"グレートピレニーズ",
"サモエド",
"ポメラニアン",
"チャウ",
"キースホンド",
"ブラバンソングリフォン",
"ペンブローク",
"カーディガン",
"トイプードル",
"ミニチュアプードル",
"スタンダードプードル",
"メキシカン・ヘアーレス",
"シンリンオオカミ",
"白いオオカミ",
"レッドウルフ",
"コヨーテ",
"ディンゴ",
"ドール",
"リカオン",
"ハイエナ",
"アカギツネ",
"キットキツネ",
"ホッキョクギツネ",
"灰色のキツネ",
"タビー",
"虎猫",
"ペルシャ猫",
"シャム猫",
"エジプトの猫",
"クーガー",
"オオヤマネコ",
"ヒョウ",
"ユキヒョウ",
"ジャガー",
"ライオン",
"虎",
"チーター",
"ヒグマ",
"アメリカクロクマ",
"氷のクマ",
"ナマケグマ",
"マングース",
"ミーアキャット",
"ハンミョウ",
"てんとう虫",
"グランドビートル",
"カミキリムシ",
"ハムシ",
"フンコロガシ",
"サイハムシ",
"ゾウムシ",
"ハエ",
"蜂",
"蟻",
"バッタ",
"クリケット",
"杖",
"ゴキブリ",
"カマキリ",
"蝉",
"ヨコバイ",
"クサカゲロウ",
"トンボ",
"イトトンボ",
"提督",
"リングレット",
"君主",
"モンシロチョウ",
"硫黄蝶",
"シジミチョウ",
"ヒトデ",
"うに",
"ナマコ",
"木のウサギ",
"野ウサギ",
"アンゴラ",
"ハムスター",
"ヤマアラシ",
"キツネリス",
"マーモット",
"ビーバー",
"モルモット",
"栗色",
"シマウマ",
"豚",
"イノシシ",
"イボイノシシ",
"カバ",
"雄牛",
"水牛",
"バイソン",
"ラム",
"ビッグホーン",
"アイベックス",
"ハーテビースト",
"インパラ",
"ガゼル",
"アラビアラクダ",
"ラマ",
"イタチ",
"ミンク",
"ケナガイタチ",
"クロアシイタチ",
"カワウソ",
"スカンク",
"狸",
"アルマジロ",
"ミユビナマケモノ",
"オランウータン",
"ゴリラ",
"チンパンジー",
"テナガザル",
"フクロテナガザル",
"オナガザル",
"パタス",
"ヒヒ",
"マカク",
"ヤセザル",
"コロブス属",
"テングザル",
"マーモセット",
"オマキザル",
"ホエザル",
"ティティ",
"クモザル",
"リスザル",
"マダガスカル猫",
"インドリ",
"インドゾウ",
"アフリカゾウ",
"レッサーパンダ",
"ジャイアントパンダ",
"バラクータ",
"ウナギ",
"ギンザケ",
"岩の美しさ",
"クマノミ",
"チョウザメ",
"ガー",
"ミノカサゴ",
"フグ",
"そろばん",
"アバヤ",
"アカデミックガウン",
"アコーディオン",
"アコースティックギター",
"空母",
"旅客機",
"飛行船",
"祭壇",
"救急車",
"両生類",
"アナログ時計",
"養蜂場",
"エプロン",
"ごみ入れ",
"アサルトライフル",
"バックパック",
"ベーカリー",
"平均台",
"バルーン",
"ボールペン",
"バンドエイド",
"バンジョー",
"バニスター",
"バーベル",
"理髪店の椅子",
"理髪店",
"納屋",
"バロメーター",
"バレル",
"バロー",
"野球",
"バスケットボール",
"バシネット",
"ファゴット",
"水泳帽",
"バスタオル",
"バスタブ",
"ビーチワゴン",
"ビーコン",
"ビーカー",
"ベアスキン",
"ビール瓶",
"ビールグラス",
"ベルコート",
"ビブ",
"自転車",
"ビキニ",
"バインダー",
"双眼鏡",
"巣箱",
"ボートハウス",
"ボブスレー",
"ループタイ",
"ボンネット",
"本棚",
"書店",
"瓶のキャップ",
"弓",
"ちょうネクタイ",
"真鍮",
"ブラジャー",
"防波堤",
"胸当て",
"ほうき",
"バケツ",
"バックル",
"防弾チョッキ",
"新幹線",
"精肉店",
"タクシー",
"大釜",
"キャンドル",
"大砲",
"カヌー",
"缶切り",
"カーディガン",
"車のミラー",
"回転木馬",
"大工のキット",
"カートン",
"車のホイール",
"現金自動預け払い機",
"カセット",
"カセット・プレーヤー",
"城",
"カタマラン",
"CDプレーヤー",
"チェロ",
"スマートフォン",
"鎖",
"チェーンリンクフェンス",
"チェーンメール",
"チェーンソー",
"胸",
"シフォニア",
"チャイム",
"中国キャビネット",
"クリスマスの靴下",
"教会",
"映画",
"クリーバー",
"崖の住居",
"マント",
"クロッグ",
"カクテルシェーカー",
"コーヒーマグ",
"コーヒーポット",
"コイル",
"ダイヤル錠",
"コンピュータのキーボード",
"製菓",
"コンテナ船",
"コンバーチブル",
"コークスクリュー",
"コルネット",
"カウボーイブーツ",
"カウボーイハット",
"クレードル",
"クレーン",
"クラッシュヘルメット",
"木箱",
"ベビーベッド",
"クロークポット",
"クロケットボール",
"松葉杖",
"胸当て",
"ダム",
"机",
"デスクトップコンピューター",
"ダイヤル電話",
"おむつ",
"デジタル時計",
"デジタル腕時計",
"ダイニングテーブル",
"意気地なし",
"食器洗い機",
"ディスクブレーキ",
"ドック",
"犬ぞり",
"ドーム",
"玄関マット",
"掘削基地",
"ドラム",
"ドラムスティック",
"ダンベル",
"ダッチオーブン",
"扇風機",
"エレキギター",
"電気機関車",
"娯楽施設",
"封筒",
"エスプレッソマシーン",
"フェースパウダー",
"フェザーボア",
"ファイル",
"消防艇",
"消防車",
"ファイアースクリーン",
"旗竿",
"フルート",
"折り畳み式椅子",
"フットボールヘルメット",
"フォークリフト",
"噴水",
"万年筆",
"四柱",
"貨車",
"フレンチホルン",
"フライパン",
"毛皮のコート",
"ごみ収集車",
"ガスマスク",
"ガソリンポンプ",
"ゴブレット",
"ゴーカート",
"ゴルフボール",
"ゴルフカート",
"ゴンドラ",
"ゴング",
"ガウン",
"グランドピアノ",
"温室",
"グリル",
"食料品店",
"ギロチン",
"ヘアスライド",
"ヘアスプレー",
"半トラック",
"ハンマー",
"妨げます",
"ハンドブロワー",
"タブレット",
"ハンカチ",
"ハードディスク",
"ハーモニカ",
"ハープ",
"ハーベスタ",
"斧",
"ホルスター",
"ホームシアター",
"ハニカム",
"フック",
"フープスカート",
"水平バー",
"馬車",
"砂時計",
"アイフォーン",
"鉄",
"ジャックオーランタン",
"ジーンズ",
"ジープ",
"ジャージー",
"ジグソーパズル",
"人力車",
"ジョイスティック",
"着物",
"膝パッド",
"結び目",
"白衣",
"ひしゃく",
"ランプのかさ",
"ノートパソコン",
"芝刈り機",
"レンズキャップ",
"レターオープナー",
"ライブラリ",
"救命ボート",
"ライター",
"リムジン",
"ライナー",
"口紅",
"ローファー",
"ローション",
"スピーカー",
"ルーペ",
"製材所",
"磁気コンパス",
"郵袋",
"メールボックス",
"マイヨ",
"マイヨ",
"マンホールの蓋",
"マラカス",
"マリンバ",
"マスク",
"マッチ棒",
"メイポール",
"迷路",
"計量カップ",
"薬箱",
"巨石",
"マイク",
"マイクロ波",
"軍服",
"ミルク缶",
"ミニバス",
"ミニスカート",
"ミニバン",
"ミサイル",
"ミトン",
"ミキシングボウル",
"移動住宅",
"モデルT",
"モデム",
"修道院",
"モニター",
"モペット",
"モルタル",
"モルタルボード",
"モスク",
"蚊帳",
"スクーター",
"マウンテンバイク",
"山のテント",
"マウス",
"ネズミ捕り",
"引っ越しトラック",
"銃口",
"ネイル",
"ネックブレース",
"ネックレス",
"乳首",
"ノート",
"オベリスク",
"オーボエ",
"オカリナ",
"オドメーター",
"オイルフィルター",
"器官",
"オシロスコープ",
"オーバースカート",
"牛車",
"酸素マスク",
"パケット",
"パドル",
"パドルホイール",
"南京錠",
"絵筆",
"パジャマ",
"宮殿",
"パンパイプ",
"ペーパータオル",
"パラシュート",
"平行棒",
"公園のベンチ",
"パーキングメーター",
"乗用車",
"パティオ",
"有料電話",
"台座",
"筆箱",
"鉛筆削り",
"香水",
"ペトリ皿",
"コピー機",
"選ぶ",
"スパイク付き鉄かぶと",
"杭柵",
"拾う",
"桟橋",
"貯金箱",
"錠剤瓶",
"枕",
"ピンポン球",
"風車",
"海賊",
"ピッチャー",
"飛行機",
"プラネタリウム",
"ビニール袋",
"皿立て",
"プラウ",
"プランジャー",
"ポラロイドカメラ",
"ポール",
"警察車",
"ポンチョ",
"ビリヤード台",
"ポップ・ボトル",
"ポット",
"ろくろ",
"パワードリル",
"礼拝用敷物",
"プリンタ",
"刑務所",
"発射体",
"プロジェクター",
"パック",
"サンドバッグ",
"財布",
"クイル",
"キルト",
"レーサー",
"ラケット",
"ラジエーター",
"無線",
"電波望遠鏡",
"天水桶",
"RV車",
"リール",
"レフレックスカメラ",
"冷蔵庫",
"リモコン",
"レストラン",
"リボルバー",
"ライフル",
"ロッキングチェア",
"焼肉料理店",
"消しゴム",
"ラグビーボール",
"ルール",
"ランニングシューズ",
"安全",
"安全ピン",
"塩の入れ物",
"サンダル",
"サロン",
"サックス",
"鞘",
"規模",
"スクールバス",
"スクーナー",
"スコアボード",
"画面",
"スクリュー",
"ドライバー",
"シートベルト",
"ミシン",
"シールド",
"靴屋",
"障子",
"買い物かご",
"ショッピングカート",
"シャベル",
"シャワーキャップ",
"シャワーカーテン",
"スキー",
"スキーマスク",
"寝袋",
"計算尺",
"引き戸",
"スロット",
"スノーケル",
"スノーモービル",
"除雪機",
"ソープディスペンサー",
"サッカーボール",
"靴下",
"太陽の皿",
"ソンブレロ",
"スープ皿",
"スペースキー",
"スペースヒーター",
"スペースシャトル",
"へら",
"スピードボート",
"クモの巣",
"スピンドル",
"スポーツカー",
"スポットライト",
"ステージ",
"蒸気機関車",
"鋼アーチ橋",
"スチールドラム",
"聴診器",
"ストール",
"石垣",
"ストップウォッチ",
"レンジ",
"ストレーナー",
"路面電車",
"ストレッチャー",
"スタジオソファ",
"仏舎利塔",
"潜水艦",
"スーツ",
"日時計",
"サングラス",
"サングラス",
"日焼け止め剤",
"つり橋",
"綿棒",
"トレーナー",
"海パン",
"スイング",
"スイッチ",
"注射器",
"電気スタンド",
"タンク",
"テーププレーヤー",
"ティーポット",
"テディ",
"テレビ",
"テニスボール",
"サッチ",
"劇場のカーテン",
"指ぬき",
"脱穀機",
"王位",
"瓦屋根",
"トースター",
"タバコ屋",
"便座",
"トーチ",
"トーテムポール",
"レッカー車",
"玩具屋",
"トラクター",
"トレーラートラック",
"トレイ",
"トレンチコート",
"三輪車",
"三胴船",
"三脚",
"凱旋門",
"トロリーバス",
"トロンボーン",
"バスタブ",
"回転ドア",
"タイプライターのキーボード",
"傘",
"一輪車",
"直立",
"真空",
"花瓶",
"ボールト",
"ベルベット",
"自動販売機",
"祭服",
"高架橋",
"バイオリン",
"バレーボール",
"ワッフル焼き型",
"壁時計",
"財布",
"ワードローブ",
"戦闘機",
"洗面器",
"ワッシャー",
"水筒",
"水差し",
"給水塔",
"ウイスキージャグ",
"ホイッスル",
"かつら",
"窓網戸",
"ブラインド",
"ウィンザーネクタイ",
"ワインボトル",
"翼",
"中華鍋",
"木製スプーン",
"ウール",
"ワームフェンス",
"難破船",
"ヨール",
"パオ",
"サイト",
"コミックブック",
"クロスワードパズル",
"道路標識",
"交通信号灯",
"ブックカバー",
"メニュー",
"プレート",
"グアカモーレ",
"コンソメ",
"ホットポット",
"パフェ",
"アイスクリーム",
"アイスキャンディー",
"フランスパン",
"ベーグル",
"プレッツェル",
"チーズバーガー",
"ホットドッグ",
"マッシュポテト",
"キャベツ",
"ブロッコリー",
"カリフラワー",
"ズッキーニ",
"そうめんかぼちゃ",
"ドングリかぼちゃ",
"カボチャ",
"キュウリ",
"アーティチョーク",
"ピーマン",
"カルドン",
"キノコ",
"リンゴ",
"イチゴ",
"オレンジ",
"レモン",
"イチジク",
"パイナップル",
"バナナ",
"パラミツ",
"カスタードアップル",
"ザクロ",
"干し草",
"カルボナーラ",
"チョコレートソース",
"パン生地",
"ミートローフ",
"ピザ",
"ポットパイ",
"ブリトー",
"赤ワイン",
"エスプレッソ",
"カップ",
"エッグノッグ",
"アルプス",
"バブル",
"崖",
"サンゴ礁",
"間欠泉",
"湖畔",
"岬",
"砂州",
"海岸",
"谷",
"火山",
"野球選手",
"新郎",
"スキューバダイバー",
"菜種",
"デイジー",
"蘭",
"トウモロコシ",
"ドングリ",
"ヒップ",
"トチノキ",
"サンゴ菌",
"ハラタケ",
"シャグマアミガサタケ",
"スッポンタケ",
"ハラタケ",
"舞茸",
"きのこ",
"耳",
"トイレットペーパー"
};

        public static string[] EMOTION_CATEGORY = {
    "angry",
    "disgust",
    "fear",
    "happy",
    "sad",
    "surprise",
    "neutral"
};

        public static string[] GENDER_CATEGORY = {
    "female","male"
};

        public static string[] COCO_CATEGORY = {
    "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train",
    "truck", "boat", "traffic light", "fire hydrant", "stop sign",
    "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow",
    "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella",
    "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard",
    "sports ball", "kite", "baseball bat", "baseball glove", "skateboard",
    "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork",
    "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange",
    "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair",
    "couch", "potted plant", "bed", "dining table", "toilet", "tv",
    "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave",
    "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase",
    "scissors", "teddy bear", "hair drier", "toothbrush"
};

        public static string[] VOC_CATEGORY = {
        "aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car",
        "cat", "chair", "cow", "diningtable", "dog", "horse", "motorbike",
        "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor"
};

        public static float[] VOC_ANCHORS = {
    1.08f, 1.19f, 3.42f, 4.41f, 6.63f, 11.38f, 9.42f, 5.11f, 16.62f, 10.52f
};

        public static float[] COCO_ANCHORS = {
	0.57273f, 0.677385f, 1.87446f, 2.06253f, 3.33843f, 5.47434f, 7.88282f, 3.52778f, 9.77052f, 9.16828f
};

    }
}