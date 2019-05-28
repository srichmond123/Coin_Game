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
const UPDATE_INTERVAL = 50; //50 ms = 20 times per second
var users = {};

io.on('connection', (socket) => {
	if (Object.keys(users).length < GAME_SIZE) {
		console.log('client connected: ' + socket.id);
		users[socket.id] = {};

		socket.on('update', (data) => {
			users[socket.id] = data;
			console.log(users);
		});

		socket.on('disconnect', () => {
			//delete users[socket.id]; //TODO Uncomment this line
		});

		if (Object.keys(users).length == GAME_SIZE) {
			//Bring users out of lobby, position at x: -4, x: 0, and x: 4:
			let xInd = -1;
			for (let id of Object.keys(users)) {
				io.to(id).emit('start', {
					id: id,
					position: {
						x: 4 * xInd++,
						y: 0,
						z: 0,
					}
				}); //<-- Tell each person their own id
			}
			update();
		}
	}
});

const update = () => {
	for (let id of Object.keys(users)) {
		io.to(id).emit('update', users);
	}
	setTimeout(update, UPDATE_INTERVAL);
}


server.listen(port, () => console.log(`listening on port ${port}`));

