'use strict';

exports.resp2 = function (event, context) {
   if (context.clientContext.contentType) { 
      context.succeed(event);
   } else {
      const response = {
         message: "Hello"
      };   
      context.succeed(response);
   }
};

