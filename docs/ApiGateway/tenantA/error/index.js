'use strict';

exports.error = function (event, context) {

    context.fail(context.apigwResponse(555, {"Content-Type": "application/json"}, {"message":"world"})); 

};
