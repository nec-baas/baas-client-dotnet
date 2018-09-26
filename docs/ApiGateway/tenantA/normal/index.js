'use strict';

exports.normal = function (event, context) {

    context.succeed(context.apigwResponse(299, {"Content-Type": "application/json"}, {"message":"world"})); 

};
