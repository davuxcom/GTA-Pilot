var frida = require('frida');
var fs = require('fs');
var path = require('path');
var net = require('net');

var realScript = fs.readFileSync(path.join(__dirname, "xboxapp.js"),{ encoding: 'utf8' });
 
mainScript = null;

var proc = parseInt(process.argv[2], 10)

console.log("ATTACHING... " + proc);

frida.attach(proc)
.then(function (session) {
  console.log('attached:', session);
  return session.createScript(realScript);
})
.then(function (script) {
  console.log('script created:', script);
	mainScript = script;
  script.load()
  .then(function () {
    console.log('script loaded');
  });
})
.catch(function (error) {
  console.log('error:', error.message);
});

function ResetServer()
{
	console.log("SERVER OK");
	var server = net.createServer(function(stream) {
	  stream.on('data', function(c) {
		  try
		  {
		//  console.log("data: " + c.toString());
		
		var str = c.toString();
		
		var parts = str.split('\n');
		if (parts.length > 0)
		{
			for (var i = 0; i < parts.length; i++) {
				if (parts[i])
				{
				mainScript.post(parts[i]);
				}
			}
		}
		else
		{
			mainScript.post(str);
		}
		
		
	//	console.log("Data Post OK");
		  }catch(e) { console.log(e) }
	  });
	  stream.on('end', function() {
		  		  try
		  {
		server.close();
		ResetServer();
				  }catch(e) { console.log(e) }
	  });
	});
	server.listen(3377, '127.0.0.1');
}





ResetServer();