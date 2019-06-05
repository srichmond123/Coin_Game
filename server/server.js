const express = require('express');
const session = require('express-session');
const http = require('http');
const socketIO = require('socket.io');
//mee
var generation = require('./generation');

//const fs = require('fs');

const port = process.env.PORT || 4001;
const app = express();  
//app.set('trust proxy', 1);
app.set('port', (process.env.PORT || 4001));
const server = http.createServer(app);
const io = socketIO(server);
io.set('transports', ['websocket']);

const GAME_SIZE = 3;
const UPDATE_INTERVAL = 150; // (In milliseconds)
const GAME_LENGTH = 1000 * 60 * 1; // 10 minutes
const COINS_PER_PLAYER = 70;
const NUM_CLUMPS = 10; //(per player);
const NEW_CLUMP_MIN_TIME = 2000;
const NEW_CLUMP_MAX_TIME = 5000;
var users = {}; //id as index, position and rotation stored
var coinCount = {}; //id -> numOwnCoins, numOtherCoins
var coinsToRegenerate = COINS_PER_PLAYER / NUM_CLUMPS; //Changes randomly each assignment

var coins = {}; //id: coins: [vec1, vec2, ...].
var MAP_ORIGIN = { 
	x: -4.88, 
	y: 4.5976, //-0.98 = old y
	z: -0.978
};
var MAP_SCALE = { 
	//x: 10.0,
	//z: 18.296 
	x: 100.0,
	z: 100.0,
}; //Generate on 2D surface, raise y according to terrain dim

/*
 * ^This doesn't update every update interval, 
 * but every time a client requests a coin they run into,
 * or tries to send a coin. Whether they can do all this or not is
 * client side decided (probably).
 */

var trial = 0; //Will be incremented, used to choose network topology
//var endzone = {}; //each entry = {id: finished (true/false)} //Each game is timed now
var game_num = 0; 
/*
 * TODO Initialize value of game_num based on database entry of games
 * played, or txt/json file stored on local computer.
 */

var coinQueue = {};

io.on('connection', (socket) => {
	if (Object.keys(users).length < GAME_SIZE) {
		console.log('client connected: ' + socket.id);
		users[socket.id] = {};
		coinCount[socket.id] = {};

		socket.on('update', (data) => {
			users[socket.id] = data;
		});

		/*
		socket.on('finish', () => {
			// Keep count of users done, if == GAME_SIZE and trial <= 2, call start method,
			// otherwise, set trial = 0, disconnect everyone
			endzone[socket.id] = true;
			console.log(socket.id + " finished");
			if (Object.keys(endzone).length == GAME_SIZE) {
				//Start new game
				console.log("everyone's done");
				endzone = {};
				if (trial <= 2) {
					start();
					sendCoins();
				} else { //All 3 trials done, notify everyone, break socket connections.
					all_ids = Object.keys(users);
					for (let id of all_ids) {
						io.to(id).emit('getOut');
						//delete users[id]; //Will happen when they call socket.Disconnect
						delete endzone[id];
					}
					trial = 0;
				}
			}
		});
		*/

		socket.on('disconnect', () => {
			//delete users[socket.id]; //TODO Uncomment these lines, broadcast to everybody that somebody left and experiment's over
			//delete endzone[socket.id];
			//console.log('client disconnected');
			//CLEAR UPDATE INTERVAL
		});

		socket.on('collect', (data) => {
			//coinCount[socket.id].numOwnCoins == undefined ? coinCount[socket.id].numOwnCoins = 1 : coinCount[socket.id].numOwnCoins++;
			for (let id of Object.keys(users)) {
				if (id != socket.id) {
					io.to(id).emit('tellCollect', {index: data.index});
				}
			}

			coins[socket.id][data.index] = undefined;
			if (coinQueue[socket.id] == undefined) {
				coinQueue[socket.id] = [];
			}
			coinQueue[socket.id].push(data.index);
			if (coinQueue[socket.id].length >= coinsToRegenerate) {
				let indices = coinQueue[socket.id];
				coinQueue[socket.id] = [];
				let positions = generation.generateClump(indices.length, MAP_ORIGIN, MAP_SCALE);
				//setTimeout(() => {
				for (let idx of indices) {
					let pos = positions.pop();
					setTimeout((position, index, myId) => {
						coins[myId][index] = position;
						for (let id of Object.keys(users)) {
							io.to(id).emit('newCoin', {id: myId, position: position, index: index});
						}
					}, getRandomInt(NEW_CLUMP_MIN_TIME, NEW_CLUMP_MAX_TIME), pos, idx, socket.id);
				}
				//}, 10000); //getRandomInt(NEW_CLUMP_MIN_TIME, NEW_CLUMP_MAX_TIME));
				coinsToRegenerate = getRandomInt(COINS_PER_PLAYER / NUM_CLUMPS - 3, COINS_PER_PLAYER / NUM_CLUMPS + 3);
			}
		});

		socket.on('claim', () => { //Only for data collection purposes, coin disappears for teammates on collect:
			coinCount[socket.id].numOwnCoins == undefined ? coinCount[socket.id].numOwnCoins = 1 : coinCount[socket.id].numOwnCoins++;
		});

		socket.on('give', (data) => {
			coinCount[data.id].numOtherCoins == undefined ? coinCount[data.id].numOtherCoins = 1 : coinCount[data.id].numOtherCoins++;
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

			///TEST CODE HERE:
			//let ids = Object.keys(users);
			//io.to(ids[2]).emit('give', {from: ids[1], to: ids[0]});
			///END TESTING
		}
	}
});

server.listen(port, () => console.log(`listening on port ${port}`));


function getRandomInt(min, max) { //Inclusive to min, max
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

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
	}
	coins = generation.generateAll(COINS_PER_PLAYER, NUM_CLUMPS, Object.keys(users), MAP_ORIGIN, MAP_SCALE);
	trial++;
	setTimeout(nextRound, GAME_LENGTH);
}

const sendCoins = () => {
	for (let id of Object.keys(users)) {
		io.to(id).emit('coins', coins);
	}
}

const nextRound = () => {
	// Keep count of users done, if == GAME_SIZE and trial <= 2, call start method,
	// otherwise, set trial = 0, disconnect everyone
	//Start new game
	console.log("Round " + trial + " over");
	if (trial <= 2) {
		start();
		sendCoins();
	} else { //All 3 trials done, notify everyone, break socket connections.
		all_ids = Object.keys(users);
		for (let id of all_ids) {
			io.to(id).emit('getOut');
		}
		trial = 0;
		game_num++;
	}
}

//Returns object of ids as key, values = array of ids they can communicate with:
const getTopology = (ids, trial) => {
	let ret = {};
	top_idx = getTopologyIndex(trial, game_num);
	switch (top_idx) {
		case 0: {
			ret[ids[0]] = [ids[1]];
			ret[ids[1]] = [ids[2]];
			ret[ids[2]] = [ids[0]];
			break;
		}
		case 1: {
			ret[ids[0]] = [ids[1]];
			ret[ids[1]] = [ids[0], ids[2]];
			ret[ids[2]] = [ids[0]];
			break;
		}
		case 2: {
			ret[ids[0]] = [ids[1], ids[2]];
			ret[ids[1]] = [ids[0], ids[2]];
			ret[ids[2]] = [ids[0], ids[1]];
			break;
		}
	}
	return ret;
}

let _arrangements = [
	[0, 1, 2], 
	[0, 2, 1],
	[1, 0, 2],
	[1, 2, 0],
	[2, 0, 1],
	[2, 1, 0]
];

const getTopologyIndex = (trial, game) => {
	//3! == 6 possible arrangements:
	return _arrangements[game_num % 6][trial];
}

const update = () => {
	for (let id of Object.keys(users)) {
		io.to(id).emit('update', users);
	}
	setTimeout(update, UPDATE_INTERVAL);
}



