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
            // Hungary
            "Pécs - Búza tér 8/a",
            "Győr - Kandó Kálmán u. 11-13.",
            "Pécs - Malomvölgyi út 2.",
            "Budapest - Hengermalom út 18.",
            // Italy
            "Milano (MI) - Via Dell'Unione, 1",
            "San Daniele del Friuli (UD) - Via Mons. Romero, 59",
            // Poland
            "Warszawa - ul. Wybrzeze Kosciuszkowskie 41",
            // Sweden
            "Malmö - Carlsgatan 22",
            // Netherlands
            "s-Hertogenbosch - Willemsplein 4",
            "Zwolle - Grote Voort 247",
            // Great Britain
            "Solihull - 2 Princes Way",
            "Nottingham - 2 Burton Street",
            // Czech Republic
            "České Budějovice - F.A. Gerstnera 2151/6",
            "Brno - Lidická 1873/36",
            "Brno - Cejl 524/42,44",
            "České Budějovice - Vrbenská 2",
        };

        public static readonly List<string> Locations = SiteLocations
            .Concat(new[] { "In WIC", "In Transit", "In Use" })
            .ToList();
    }
}

