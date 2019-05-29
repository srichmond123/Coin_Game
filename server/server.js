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

io.on('connection', (socket) => {
	if (Object.keys(users).length < GAME_SIZE) {
		console.log('client connected: ' + socket.id);
		users[socket.id] = {};

		socket.on('update', (data) => {
			users[socket.id] = data;
		});

		socket.on('disconnect', () => {
			//delete users[socket.id]; //TODO Uncomment this line
			//console.log('client disconnected');
		});

		socket.on('collect', (data) => {
			users[socket.id].numOwnCoins == undefined ? users[socket.id].numOwnCoins = 1 : users[socket.id].numOwnCoins++;
			for (let id of Object.keys(users)) {
				if (id != socket.id) {
					io.to(id).emit("tellCollect", {index: data.index});
				}
			}
		});

		socket.on('give', (data) => {
			users[socket.id].numOwnCoins--;
			users[data.id].numOtherCoins == undefined ? users[data.id].numOtherCoins = 1 : users[data.id].numOtherCoins++;
			for (let id of Object.keys(users)) { //So the coin giver can publicly virtue signal via animation
				if (id != socket.id) {
					io.to(id).emit("tellGive", {from: socket.id, to: data.id});
				}
			}
		});

		socket.on('log', (msg) => {
			console.log(msg);
		});
		
		if (Object.keys(users).length == GAME_SIZE) {
			//Bring users out of lobby, position at x: -4, x: 0, and x: 4:
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
				}); //<-- Tell each person their own id

				coins[id] = [
					{x: 0.1 + shift, y: 0.5, z: 0.1 + shift},
					{x: 0.2 + shift, y: 0.6, z: 0.4 + shift}
				];
				shift += 1.2;
			}
			update();
			sendCoins();
		}
	}
});

const sendCoins = () => {
	for (let id of Object.keys(users)) {
		io.to(id).emit('coins', coins);
	}
}

const update = () => {
	for (let id of Object.keys(users)) {
		io.to(id).emit('update', users);
	}
	setTimeout(update, UPDATE_INTERVAL);
}


server.listen(port, () => console.log(`listening on port ${port}`));

