//env (for pilots): GOAL, SCALE, COINS_PER, 
//	MIN_RANGE, RANGE_DECREASE, RANGE_INCREASE
const RELEASE = process.env.RELEASE || false; 

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
app.set('port', port);
const server = http.createServer(app);
const io = socketIO(server);
io.set('transports', ['websocket']);

const GAME_SIZE = 3;
const NUM_ROWS = 10; //GRID size
const UPDATE_INTERVAL = 100; // (In milliseconds)
//const GAME_LENGTH = 1000 * 60 * 10; // 10 minutes
const COINS_PER_PLAYER = process.env.COINS_PER || 40;

////// old code
const NUM_CLUMPS = 10; //(per player);
const NEW_CLUMP_MIN_TIME = 2000;
const NEW_CLUMP_MAX_TIME = 5000;
//////

const NUM_EMPTY_CELLS = 35;
const COUNTDOWN_TIME = 1000 * 10;
var users = {}; //id as index, position and rotation stored
var coinCount = {}; //id -> numOwnCoins, numOtherCoins
var coinsToRegenerate = COINS_PER_PLAYER / NUM_CLUMPS; //Changes randomly each assignment

var coins = {}; //id: coins: [vec1, vec2, ...].
var MAP_ORIGIN = { 
	x: -4.88, 
	y: 4.5976, //Terrain stuff, this variable isn't that important, could just be (0, 0, 0) but whatever
	z: -0.978
};
var MAP_SCALE = { 
	x: (process.env.SCALE || 300) * 1.0,
	y: 0,
	z: (process.env.SCALE || 300) * 1.0,
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
var gameScore = 0;
var startTime;
var intervalId = -1;
const GOAL = process.env.GOAL || 30;


var empties;// = getEmptyPatches(NUM_EMPTY_CELLS);

var gameBegan = false;


io.on('connection', (socket) => {
	if (Object.keys(users).length < GAME_SIZE) {
		console.log('client connected: ' + socket.id);

		users[socket.id] = {};
		coinCount[socket.id] = {};

		for (let id of Object.keys(users)) {
			io.to(id).emit('newConnection', {left: GAME_SIZE - Object.keys(users).length});
		}

		socket.on('update', (data) => {
			users[socket.id] = data;
		});

		/* //Not doing this anymore:
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
		socket.on("quit", () => {
			if (RELEASE) {
				for (let id of Object.keys(users)) {
					if (id != socket.id) {
						io.to(id).emit('getOut', {quit: true});
					}
					endGame();
				}
			}
		});

		socket.on('disconnect', () => {
			console.log('client disconnected');
		});

		socket.on('collect', (data) => {
			//coinCount[socket.id].numOwnCoins == undefined ? coinCount[socket.id].numOwnCoins = 1 : coinCount[socket.id].numOwnCoins++;
			for (let id of Object.keys(users)) {
				if (id != socket.id) {
					io.to(id).emit('tellCollect', {id: socket.id, index: data.index});
				}
			}

			if (++gameScore == GOAL) {
				nextRound();
			} else {
				let relativeIdx = getRelativeIndex(socket.id, data.index);
				generation.replaceSingleCorrelatedGrid(coins, socket.id, relativeIdx, empties, NUM_ROWS, MAP_ORIGIN, MAP_SCALE); 
				setTimeout((emission) => {
					for (let id of Object.keys(users)) {
						io.to(id).emit('newCoin', emission);
					}
				}, 1000 * 10, {id: socket.id, position: coins[socket.id].positions[relativeIdx], index: data.index});
			}
			/*
			let res = generation.generateSingleCorrelated(coins, socket.id, data.index, MAP_ORIGIN, MAP_SCALE);
			coins[socket.id][data.index] = res;
			for (let id of Object.keys(users)) {
				io.to(id).emit('newCoin', { id: socket.id, position: coins[socket.id][data.index], index: data.index });
			}
			*/
			/*
			coins[socket.id][data.index] = undefined;
			if (coinQueue[socket.id] == undefined) {
				coinQueue[socket.id] = [];
			}
			coinQueue[socket.id].push(data.index);
			if (coinQueue[socket.id].length >= coinsToRegenerate) {
				let indices = coinQueue[socket.id];
				coinQueue[socket.id] = [];
				let positions = generation.generateClump(indices.length, MAP_ORIGIN, MAP_SCALE, data.position);
				for (let idx of indices) {
					let pos = positions.pop();
					setTimeout((position, index, myId) => {
						coins[myId][index] = position;
						for (let id of Object.keys(users)) {
							io.to(id).emit('newCoin', {id: myId, position: position, index: index});
						}
					}, getRandomInt(NEW_CLUMP_MIN_TIME, NEW_CLUMP_MAX_TIME), pos, idx, socket.id);
				}
				coinsToRegenerate = getRandomInt(COINS_PER_PLAYER / NUM_CLUMPS - 3, COINS_PER_PLAYER / NUM_CLUMPS + 3);
			}
			*/
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
		
		if (Object.keys(users).length == GAME_SIZE && !gameBegan) { //<-- To prevent rapid disconnect -> reconnect from ending game
			//Bring users out of lobby, position at x: -4, x: 0, and x: 4:
			start();
			startTime = -1;
			update();
			setTimeout(() => sendCoins(), COUNTDOWN_TIME);
			gameBegan = true;

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

const getRelativeIndex = (myId, absIndex) => {
	let curr = absIndex;
	for (let id of Object.keys(coins)) {
		if (id != myId) {
			curr -= coins[id].positions.length;
		} else {
			return curr;
		}
	}
	return absIndex;
}

const start = () => {
	//shift variable below is TEST code, a placeholder for coin generation
	//let xInd = -1;
	let shift = 0;
	let topology = getTopology(Object.keys(users), trial);
	for (let id of Object.keys(users)) {
		let position = {
			x: Math.random() * MAP_SCALE.x + MAP_ORIGIN.x,
			y: 0,
			z: Math.random() * MAP_SCALE.z + MAP_ORIGIN.z,
		};
		io.to(id).emit('start', {
			id: id,
			position,
			goal: GOAL,
			minRange: (process.env.MIN_RANGE || 35) * 1.0,
			rangeDecrease: (process.env.RANGE_DECREASE || 2) * 1.0,
			rangeIncrease: (process.env.RANGE_INCREASE || 22) * 1.0,
			topology,
			origin: MAP_ORIGIN,
			scale: MAP_SCALE, //Allows players to figure out bounds for minimap and out of bounds warnings
		});
		coinCount[id].numOwnCoins = 0;
		coinCount[id].numOtherCoins = 0;
	}
	//coins = generation.generateAll(COINS_PER_PLAYER, NUM_CLUMPS, Object.keys(users), MAP_ORIGIN, MAP_SCALE);
	//coins = generation.generateCorrelatedRandom(COINS_PER_PLAYER, Object.keys(users), MAP_ORIGIN, MAP_SCALE);
	//coins = generation.generatePoisson(1, 100, Object.keys(users), MAP_ORIGIN, MAP_SCALE);
	empties = getEmptyPatches(NUM_EMPTY_CELLS);
	coins = generation.generateCorrelatedGrid(COINS_PER_PLAYER, NUM_ROWS, Object.keys(users), empties, MAP_ORIGIN, MAP_SCALE);
	coinQueue = {};
	//setTimeout(nextRound, GAME_LENGTH);
}

const sendCoins = () => {
	let sendCoins = {};
	for (let id of Object.keys(coins)) {
		if (coins[id].positions) {
			sendCoins[id] = coins[id].positions;
		} else {
			sendCoins[id] = coins[id];
		}
	}
	for (let id of Object.keys(users)) {
		io.to(id).emit('coins', sendCoins);
	}
}

const nextRound = () => {
	// Keep count of users done, if == GAME_SIZE and trial <= 2, call start method,
	// otherwise, set trial = 0, disconnect everyone
	//Start new game
	console.log("Round " + (trial + 1) + " over");
	gameScore = 0;
	if (trial < 2) {
		start();
		startTime = -1;
		sendCoins();
		trial++;
	} else { //All 3 trials done, notify everyone, break socket connections.
		all_ids = Object.keys(users);
		for (let id of all_ids) {
			io.to(id).emit('getOut', {quit: false});
		}
		endGame();
	}
}

//Returns object of ids as key, values = array of ids they can communicate with:
const getTopology = (ids, trial) => {
	let ret = {};
	let randIds = shuffle(ids);
	top_idx = getTopologyIndex(trial, game_num);
	switch (top_idx) {
		case 0: {
			ret[randIds[0]] = [randIds[1]];
			ret[randIds[1]] = [randIds[2]];
			ret[randIds[2]] = [randIds[0]];
			break;
		}
		case 1: {
			ret[randIds[0]] = [randIds[1]];
			ret[randIds[1]] = [randIds[0], randIds[2]];
			ret[randIds[2]] = [randIds[0]];
			break;
		}
		case 2: {
			ret[randIds[0]] = [randIds[1], randIds[2]];
			ret[randIds[1]] = [randIds[0], randIds[2]];
			ret[randIds[2]] = [randIds[0], randIds[1]];
			break;
		}
	}
	return ret;
}

function shuffle(a) {
	var j, x, i;
	for (i = a.length - 1; i > 0; i--) {
		j = Math.floor(Math.random() * (i + 1));
		x = a[i];
		a[i] = a[j];
		a[j] = x;
	}
	return a;
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
	if (startTime == -1) startTime = Date.now();
	let time = Date.now() - startTime;
	for (let id of Object.keys(users)) {
		io.to(id).emit('update', {users, time});
	}
	intervalId = setTimeout(update, UPDATE_INTERVAL);
}


const getEmptyPatches = (num) => {
	let res = [];
	let all = [];
	for (let i = 0; i < NUM_ROWS; i++) {
		for (let j = 0; j < NUM_ROWS; j++) {
			all.push([i, j]);
		}
	}
	for (let i = 0; i < num; i++) {
		res.push(all.splice(getRandomInt(0, all.length - 1), 1)[0]);
	}
	return res;
}

const endGame = () => {
	startTime = -1;
	clearTimeout(intervalId);
	intervalId = -1;
	trial = 0;
	users = {};
	game_num++;
	gameBegan = false;
}

