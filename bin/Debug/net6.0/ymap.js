ymaps.ready(init);

var data = [{"Id": 5936371933161205840, "Longitude": 82.91398145000001, "Latitude": 55.04570045}, {"Id": 6116702820854175916, "Longitude": 82.91409180000001, "Latitude": 55.04570953333334}, {"Id": 1126495516822629813, "Longitude": 82.91420215000001, "Latitude": 55.04571861666667}, {"Id": 6448972652, "Longitude": 82.91431250000001, "Latitude": 55.0457277}, {"Id": 1049186304577744190, "Longitude": 82.91441275000001, "Latitude": 55.04573575}, {"Id": 6448972652, "Longitude": 82.91431250000001, "Latitude": 55.0457277}, {"Id": 5733706139820851878, "Longitude": 82.914513, "Latitude": 55.0457438}, {"Id": 3276219690219691834, "Longitude": 82.91461325, "Latitude": 55.04575185}, {"Id": 1183457767, "Longitude": 82.9147135, "Latitude": 55.0457599}, {"Id": 8289867232359572843, "Longitude": 82.91498095, "Latitude": 55.04578375}];
var cleanData = [];
var queryCoords = [];
var Http = new XMLHttpRequest();
var myMap;
var isDangerSelect = false;
var area;

// Give distance in meters
function distance(coords)
{
    var R = 6371008.7714;
    var x = ((Math.PI / 180) * coords[1][1] - (Math.PI / 180) * coords[0][1]) * Math.cos(0.5 * ((Math.PI / 180) * coords[1][0] + (Math.PI / 180) * coords[0][0]));
    var y = (Math.PI / 180) * coords[1][0] - (Math.PI / 180) * coords[0][0];
    return R * Math.sqrt(x * x + y * y);
}

// Callback from server
Http.onreadystatechange = function() 
{
    if (this.readyState == 4 && this.status == 200)
        {
            data = JSON.parse(Http.responseText);
            console.log(data.length);
            if (data.route === "No path was found")
            {
                alert("Маршрут не был найден");
            }
            else
            {
                if (!isDangerSelect)
                {
                    update();
                }
                else
                {
                    
                }
            }
        }
}

function update()
{
    cleanData = [];
    for (let i = 0; i < (data.length - 1); i += 1)
    {
        if (data[i].Danger <= 13)
        {
            cleanData.push(
                new ymaps.Polyline(
                        [
                            [data[i].Latitude, data[i].Longitude], 
                            [data[i + 1].Latitude, data[i + 1].Longitude]
                        ], {},
                        {
                            strokeColor: ["#00afb9"], 
                            strokeWidth: [5]
                        }
                    )
                );
        }
        else if(data[i].Danger > 13 && data[i].Danger <= 70)
        {
            cleanData.push(
                new ymaps.Polyline(
                        [
                            [data[i].Latitude, data[i].Longitude], 
                            [data[i + 1].Latitude, data[i + 1].Longitude]
                        ], {},
                        {
                            strokeColor: ["#fbbfa3"], 
                            strokeWidth: [5]
                        }
                    )
                );
        }
        else
        {
            cleanData.push(
                new ymaps.Polyline(
                        [
                            [data[i].Latitude, data[i].Longitude], 
                            [data[i + 1].Latitude, data[i + 1].Longitude]
                        ], {},
                        {
                            strokeColor: ["#f48b7b"], 
                            strokeWidth: [5]
                        }
                    )
                );
        }
    }
    for (var way of cleanData)
        myMap.geoObjects.add(way);
}

function init(){
	document.getElementById("map").style.height = Math.floor(window.innerHeight * 0.8) + "px";
    myMap = new ymaps.Map("map", {
        center: [55.068, 82.967],
        zoom: 10 //15
    }, {autoFitViewport: 'always'});
    area = new ymaps.GeoObject(
    {
        geometry:
        {
            type: 'LineString',
            coordinates:
            [
            [55.1019, 82.8952],
            [55.1019, 83.0061],
            [55.0260, 83.0061],
            [55.0260, 82.8952],
            [55.1019, 82.8952]
            ]
        },
        properties:
        {
        hintContent: 'Область работы алгоритма'
        }
    },
    {
        strokeColor: '#00afb9',
        strokeOpacity: 0.3,
        strokeWidth: 2
    });
    myMap.geoObjects.add(area);
    myMap.events.add('click', function (e) 
    {
        queryCoords.push(e.get('coords'));
        if(queryCoords.length === 3)
        {
            myMap.geoObjects.removeAll();
            myMap.geoObjects.add(area);
            queryCoords = [];
        }
        else
        { 
            if (!isDangerSelect)
            {
                if (queryCoords.length === 1)
                    myMap.geoObjects.add(new ymaps.Placemark(queryCoords[0], 
                        {
                            balloonContent: "От"
                        },
                        {
                            preset: 'islands#dotIcon',
                            iconColor: '#a2d2ff'
                        }));
                else
                {
                    if (queryCoords.length === 2)
                    {
                        myMap.geoObjects.add(new ymaps.Placemark(queryCoords[1],
                            {
                                balloonContent: "До"
                            },
                            {
                                preset: 'islands#dotIcon',
                                iconColor: '#ffafcc'
                            }));
                        Http.open("GET", `/FindPath?latfrom=${queryCoords[0][0]}&longfrom=${queryCoords[0][1]}&latto=${queryCoords[1][0]}&longto=${queryCoords[1][1]}`); // Add here coordinates
                        Http.send();
                    }
                }
            }
            else
            {
                if (queryCoords.length === 1)
                    myMap.geoObjects.add(new ymaps.Placemark(queryCoords[0],
                        {
                            ballonContent: "Выбор района опасности"
                        },
                        {
                            preset: 'islands#blueCircleDotIconWithCaption',
                            iconColor: '#f07167'
                        }));
                else
                {
                    if (queryCoords.length === 2)
                    {
                        myMap.geoObjects.add(new ymaps.Circle([queryCoords[0], distance(queryCoords)],
                        {},
                        {
                            fillColor: '#f7a58f',
                            strokeColor: '#f07167',
                            fillOpacity: 0.7,
                            strokeWidth: 4
                        }));
                        Http.open("GET", `/AddDanger?dangertype=${document.getElementById("danger-select").value}&latO=${queryCoords[0][0]}&longO=${queryCoords[0][1]}&r=${distance(queryCoords)}`); 
                        Http.send();
                    }
                }
            }
        }
    }
    )
	
}

onresize = (event) => {document.getElementById("map").style.height = Math.floor(window.innerHeight * 0.8) + "px"};
document.getElementById("danger-select").onchange = (event) => 
{
    isDangerSelect = (event.target.value != "");
    myMap.geoObjects.removeAll();
    myMap.geoObjects.add(area);
    queryCoords = [];
};

let icons = document.getElementsByClassName("icon");

for (let icon of icons)
{
    let enabled = true;
    icon.onclick = (event) =>
    {
        if (!enabled)
            return;
            enabled = false;
        
        if (icon.style.rotate !== "90deg")
        {
            let angle = 0;
            let idi = setInterval(() => {
                angle += 5; 
                icon.style.rotate = angle + "deg";
                if (angle === 90)
                    {
                        clearInterval(idi);
                        enabled = true;
                    }
            }, 5);
            document.getElementById(icon.id + "text").style.display = null;
        }
        else
        {
            let angle = 90;
            let idi = setInterval(() => {
                angle -= 5; 
                icon.style.rotate = angle + "deg";
                if (angle === 0)
                    {
                        clearInterval(idi);
                        enabled = true;
                    }
            }, 5);
            document.getElementById(icon.id + "text").style.display = "none";
        }
    }
}