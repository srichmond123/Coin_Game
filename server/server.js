const express = require('express');
const session = require('express-session');
const http = require('http');
const socketIO = require('socket.io');
//const fs = require('fs');

const port = process.env.PORT || 4001;
const app = express();  
//app.set('trust proxy', 1);
app.set('port', (process.env.PORT || 4001));
const server = http.createServer(app);
const io = socketIO(server);
io.set('transports', ['websocket']);

const GAME_SIZE = 3;
const UPDATE_INTERVAL = 150; //50 ms = 20 times per second
var users = {}; //id as index, position and rotation stored

var coins = {}; //id: coins: [vec1, vec2, ...].
//^^This doesn't update every update interval, 
// but every time a client requests a coin they run into,
// or tries to send a coin. Whether they can do all this or not is
// client side decided (probably).

var trial = 1; //Will be incremented, used to choose network topology
var endzone = {}; //each entry = {id: finished (true/false)}

io.on('connection', (socket) => {
	if (Object.keys(users).length < GAME_SIZE) {
		console.log('client connected: ' + socket.id);
		users[socket.id] = {};

		socket.on('update', (data) => {
			users[socket.id] = data;
		});

		socket.on('finish', () => {
			// Keep count of users done, if == GAME_SIZE and trial <= 3, call start method,
			// otherwise, set trial = 0, disconnect everyone
			endzone[socket.id] = true;
			console.log(socket.id + " finished");
			if (Object.keys(endzone).length == GAME_SIZE) {
				//Start new game
				console.log("everyone's done");
				endzone = {};
				if (trial <= 3) {
					start();
					sendCoins();
				} else { //All 3 trials done, notify everyone, break socket connections.
					all_ids = Object.keys(users);
					for (let id of all_ids) {
						io.to(id).emit('getOut');
						delete users[id];
						delete endzone[id];
					}
				}
			}
		});

		socket.on('disconnect', () => {
			//delete users[socket.id]; //TODO Uncomment these lines, broadcast to everybody that somebody left and experiment's over
			//delete endzone[socket.id];
			//console.log('client disconnected');
		});

		socket.on('collect', (data) => {
			users[socket.id].numOwnCoins == undefined ? users[socket.id].numOwnCoins = 1 : users[socket.id].numOwnCoins++;
			for (let id of Object.keys(users)) {
				if (id != socket.id) {
					io.to(id).emit('tellCollect', {index: data.index});
				}
			}
		});

		socket.on('give', (data) => {
			users[socket.id].numOwnCoins--;
			users[data.id].numOtherCoins == undefined ? users[data.id].numOtherCoins = 1 : users[data.id].numOtherCoins++;
			// So the coin giver can publicly virtue signal via animation to the one other person,
			// or, coin receiver can know (if data.to == their id)
			for (let id of Object.keys(users)) {
				if (id != socket.id) {
					io.to(id).emit('give', {from: socket.id, to: data.id});
				}
			}
		});

		socket.on('log', (msg) => {
			console.log(msg);
		});
		
		if (Object.keys(users).length == GAME_SIZE) {
			//Bring users out of lobby, position at x: -4, x: 0, and x: 4:
			start();
			update();
			sendCoins();

			//TEST CODE HERE:
			//let ids = Object.keys(users);
			//io.to(ids[2]).emit('give', {from: ids[1], to: ids[0]});
			//END TESTING
		}
	}
});

const start = () => {
	//shift variable below is TEST code, a placeholder for coin generation
	let xInd = -1;
	let shift = 0;
	for (let id of Object.keys(users)) {
		io.to(id).emit('start', {
			id: id,
			position: {
				x: 4 * xInd++,
				y: 0,
				z: 0,
			},
			interval: UPDATE_INTERVAL * 0.001,
			topology: getTopology(Object.keys(users), trial),
		}); //<-- Tell each person their own id

		coins[id] = [
			{x: 0.1 + shift, y: 0.5, z: 0.1 + shift},
			{x: 0.2 + shift, y: 0.6, z: 0.4 + shift}
		];
		shift += 1.2;
	}
	trial++;
}

const sendCoins = () => {
	for (let id of Object.keys(users)) {
		io.to(id).emit('coins', coins);
	}
}

//Returns object of ids as key, values = array of ids they can communicate with:
const getTopology = (ids, trial) => {
	let ret = {};
	switch (trial) {
		case 1: {
			ret[ids[0]] = [ids[1]];
			ret[ids[1]] = [ids[2]];
			ret[ids[2]] = [ids[0]];
			break;
		}
		case 2: {
			ret[ids[0]] = [ids[1]];
			ret[ids[1]] = [ids[0], ids[2]];
			ret[ids[2]] = [ids[0]];
			break;
		}
		case 3: {
			ret[ids[0]] = [ids[1], ids[2]];
			ret[ids[1]] = [ids[0], ids[2]];
			ret[ids[2]] = [ids[0], ids[1]];
			break;
		}
	}
	return ret;
}

const update = () => {
	for (let id of Object.keys(users)) {
		io.to(id).emit('update', users);
	}
	setTimeout(update, UPDATE_INTERVAL);
}


server.listen(port, () => console.log(`listening on port ${port}`));

