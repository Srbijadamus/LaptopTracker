namespace LaptopTracker.Helpers
{
    public static class LocationList
    {
        public static readonly List<string> SiteLocations = new()
        {
            "Hannover - Ricklinger Stadtweg 123-127",
            "München - Arnulfstr. 203",
            "Demmin - Woldeforster Str. 6",
            "Helmstedt - Schillerstr. 3",
            "Essen - Brüsseler Platz 1",
            "Landshut - Kiem-Pauli Str. 2",
            "Hamburg - Normannenweg 9",
            "Salzgitter - Joachim-Campe-Straße 14",
            "Fürstenwalde - Langewahler Str. 60",
            "Potsdam - Am Kanal 2-3",
            "Quickborn - Schleswag-HeinGas-Platz 1",
            "Essen - ThyssenKrupp Allee 1",
            "Arnsberg - Hellefelder Str. 8",
            "Dortmund - Florianstr. 15-21",
            "Münster - Weseler Str. 480",
            "Osnabrück - Goethering 23-29",
            "Recklinghausen - Bochumer Str. 2",
            "Siegen - Friedrichstr. 60",
            "Trier - Eurener Str. 33",
            "Wesel - Reeser Landstr. 41",
            "Neuss - Collingstr. 2",
            "Neu-Isenburg - Flughafenstr. 20",
            "Brokdorf - Osterende",
            "Emmerthal - Kraftwerksgelände",
            "Stade - Bassenflether Chaussee",
            "Stadland - Dedesdorfer Straße 2",
            "Grafenrheinfeld - Kraftwerkstraße",
            "Augsburg - Schaezlerstr. 3",
            "Berlin - Gaußstr. 11",
            "Saffig - Rauschermühle",
            "Berlin - Brückenstrasse 6",
            "Saarbrücken - Heinrich-Böcking-Str. 10-14",
            "Essenbach - Dammstraße",
            "Mülheim - Moritzstr. 16-22",
            "Rendsburg - Kieler Str. 47",
            "Regensburg - Lilienthalstraße 7",
            "Bamberg - Doktor-Robert-Pfleger-Str. 20",
            "Pfaffenhofen - Draht 7",
            "Halle (Saale) - Magdeburger Str. 51",
            "Markkleeberg - Friedrich-Ebert-Str. 26",
            "Pécs - Búza tér 8/a",
            "Győr - Kandó Kálmán u. 11-13.",
            "Pécs - Malomvölgyi út 2.",
            "Budapest - Hengermalom út 18.",
            "Milano (MI) - Via Dell'Unione, 1",
            "San Daniele del Friuli (UD) - Via Mons. Romero, 59",
            "Warszawa - ul. Wybrzeze Kosciuszkowskie 41",
            "Malmö - Carlsgatan 22",
            "s-Hertogenbosch - Willemsplein 4",
            "Zwolle - Grote Voort 247",
            "Solihull - 2 Princes Way",
            "Nottingham - 2 Burton Street",
            "České Budějovice - F.A. Gerstnera 2151/6",
            "Brno - Lidická 1873/36",
            "Brno - Cejl 524/42,44",
            "České Budějovice - Vrbenská 2",
        };

        public static readonly List<string> Locations = SiteLocations
            .Concat(new[] { "In WIC", "In Transit", "In Use" })
            .ToList();

        public static readonly Dictionary<string, string> LocationCountry = new()
        {
            // Germany
            ["Hannover - Ricklinger Stadtweg 123-127"]    = "DE",
            ["München - Arnulfstr. 203"]                  = "DE",
            ["Demmin - Woldeforster Str. 6"]              = "DE",
            ["Helmstedt - Schillerstr. 3"]                = "DE",
            ["Essen - Brüsseler Platz 1"]                 = "DE",
            ["Landshut - Kiem-Pauli Str. 2"]              = "DE",
            ["Hamburg - Normannenweg 9"]                  = "DE",
            ["Salzgitter - Joachim-Campe-Straße 14"]      = "DE",
            ["Fürstenwalde - Langewahler Str. 60"]        = "DE",
            ["Potsdam - Am Kanal 2-3"]                    = "DE",
            ["Quickborn - Schleswag-HeinGas-Platz 1"]     = "DE",
            ["Essen - ThyssenKrupp Allee 1"]              = "DE",
            ["Arnsberg - Hellefelder Str. 8"]             = "DE",
            ["Dortmund - Florianstr. 15-21"]              = "DE",
            ["Münster - Weseler Str. 480"]                = "DE",
            ["Osnabrück - Goethering 23-29"]              = "DE",
            ["Recklinghausen - Bochumer Str. 2"]          = "DE",
            ["Siegen - Friedrichstr. 60"]                 = "DE",
            ["Trier - Eurener Str. 33"]                   = "DE",
            ["Wesel - Reeser Landstr. 41"]                = "DE",
            ["Neuss - Collingstr. 2"]                     = "DE",
            ["Neu-Isenburg - Flughafenstr. 20"]           = "DE",
            ["Brokdorf - Osterende"]                      = "DE",
            ["Emmerthal - Kraftwerksgelände"]             = "DE",
            ["Stade - Bassenflether Chaussee"]            = "DE",
            ["Stadland - Dedesdorfer Straße 2"]           = "DE",
            ["Grafenrheinfeld - Kraftwerkstraße"]         = "DE",
            ["Augsburg - Schaezlerstr. 3"]                = "DE",
            ["Berlin - Gaußstr. 11"]                      = "DE",
            ["Saffig - Rauschermühle"]                    = "DE",
            ["Berlin - Brückenstrasse 6"]                 = "DE",
            ["Saarbrücken - Heinrich-Böcking-Str. 10-14"] = "DE",
            ["Essenbach - Dammstraße"]                    = "DE",
            ["Mülheim - Moritzstr. 16-22"]                = "DE",
            ["Rendsburg - Kieler Str. 47"]                = "DE",
            ["Regensburg - Lilienthalstraße 7"]           = "DE",
            ["Bamberg - Doktor-Robert-Pfleger-Str. 20"]   = "DE",
            ["Pfaffenhofen - Draht 7"]                    = "DE",
            ["Halle (Saale) - Magdeburger Str. 51"]       = "DE",
            ["Markkleeberg - Friedrich-Ebert-Str. 26"]    = "DE",
            // Hungary
            ["Pécs - Búza tér 8/a"]                       = "HU",
            ["Győr - Kandó Kálmán u. 11-13."]             = "HU",
            ["Pécs - Malomvölgyi út 2."]                  = "HU",
            ["Budapest - Hengermalom út 18."]             = "HU",
            // Italy
            ["Milano (MI) - Via Dell'Unione, 1"]          = "IT",
            ["San Daniele del Friuli (UD) - Via Mons. Romero, 59"] = "IT",
            // Poland
            ["Warszawa - ul. Wybrzeze Kosciuszkowskie 41"] = "PL",
            // Sweden
            ["Malmö - Carlsgatan 22"]                     = "SE",
            // Netherlands
            ["s-Hertogenbosch - Willemsplein 4"]          = "NL",
            ["Zwolle - Grote Voort 247"]                  = "NL",
            // Great Britain
            ["Solihull - 2 Princes Way"]                  = "GB",
            ["Nottingham - 2 Burton Street"]              = "GB",
            // Czech Republic
            ["České Budějovice - F.A. Gerstnera 2151/6"]  = "CZ",
            ["Brno - Lidická 1873/36"]                    = "CZ",
            ["Brno - Cejl 524/42,44"]                     = "CZ",
            ["České Budějovice - Vrbenská 2"]             = "CZ",
        };

        public static string GetCountry(string location) =>
            LocationCountry.TryGetValue(location, out var c) ? c : "";

        public static List<string>? GetLocationsByCountry(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return null;
            return LocationCountry
                .Where(kv => kv.Value == countryCode)
                .Select(kv => kv.Key)
                .ToList();
        }
    }
}

