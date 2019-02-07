var request = require('request');

const functions = require('firebase-functions');

// // Create and Deploy Your First Cloud Functions
// // https://firebase.google.com/docs/functions/write-firebase-functions


exports.getAxieImage = functions.https.onRequest((req, res) => {
    var url = 'https://api.axieinfinity.com/v1/axies/';
    
    //response.status(200).send(req.query.axieId);
    //https://samples.openweathermap.org/data/2.5/weather?q=Dallas&appid=b6907d289e10d714a6e88b30761fae22

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
            }
        }
    });
});
