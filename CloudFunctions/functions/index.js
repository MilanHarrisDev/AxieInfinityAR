var request = require('request');

const functions = require('firebase-functions');
const url = 'https://api.axieinfinity.com/v1/axies/';

// // Create and Deploy Your First Cloud Functions
// // https://firebase.google.com/docs/functions/write-firebase-functions

//returns link to image file
exports.getAxieImage = functions.https.onRequest((req, res) => {  
    request(url + req.query.axieId, function (error, response, body) {
        if (!error && response.statusCode == 200) 
        {
            var jsonBody = JSON.parse(body);
            
            switch(req.query.type)
            {
                case 'axie':
                    res.status(200).send(jsonBody.figure.axie.image);
                    break;
                case 'static':
                    res.status(200).send(jsonBody.figure.static.idle);
                    break;
                case 'spirit':
                    res.status(200).send(jsonBody.figure.spirit.image);
                    break;
                default:
                    res.status(200).send(jsonBody.figure.axie.image);
                    break;
            }
        }
    });
});

//returns link to atlas file
exports.getAxieImageAtlas = functions.https.onRequest((req, res) =>{
    request(url + req.query.axieId, function (error, response, body) {
        if (!error && response.statusCode == 200) 
        {
            var jsonBody = JSON.parse(body);
            
            switch(req.query.type)
            {
                case 'axie':
                    res.status(200).send(jsonBody.figure.axie.atlas);
                    break;
                case 'spirit':
                    res.status(200).send(jsonBody.figure.spirit.atlas);
                    break;
                default:
                    res.status(200).send(jsonBody.figure.axie.atlas);
                    break;
            }
        }
    });
});

//returns json blob for axie spine model
exports.getAxieSpineModel = functions.https.onRequest((req, res) =>{
    request(url + req.query.axieId, function (error, response, body) {
        if (!error && response.statusCode == 200) 
        {
            var jsonBody = JSON.parse(body);
            request(jsonBody.figure.axie.spineModel, function (error, response, body) { 
                if (!error && response.statusCode == 200) 
                {
                    var spineJson = JSON.parse(body);
                    switch(req.query.type)
                    {
                        case 'bones':
                            res.status(200).send(spineJson.bones);
                            break;
                        case 'slots':
                            res.status(200).send(spineJson.slots);
                            break;
                        case 'skins':
                            var skinTypes = ['shadow', 'leg-front-right', 'leg-back-right',
                             'ear-right', 'tail', 'back', 'body', 'pattern', 'leg-front-left', 
                             'leg-back-left', 'eyes', 'mouth', 'horn', 'ear-left'];

                            var skinsArray = [];

                            skinTypes.forEach(function(element){
                                var temp = spineJson.skins.default[element][element];
                                temp["name"] = element;
                                skinsArray.push(temp);
                            });
                            res.status(200).send(skinsArray);
                            break;
                    }
                }
            });
        }
    });
});