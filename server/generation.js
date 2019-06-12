var random = require('random');

//const MIN_DIST = 0.3; //Absolute coordinates (necessary due to abs size of coin prefab)
//const GRAVITY_STRENGTH = 0.02;
const CLUMP_RADIUS = 0.06; //0 to 1 scale
const DIST_AWAY_PLAYER = 0.25;

const PROB_ATTRACTED = 0.4;

module.exports = {
	generateAll: (amount_per, num_clumps, ids, origin, scale) => {
		return rescale(get2DClumpVectors(amount_per, num_clumps, ids), origin, scale);
	},
	generateClump: (size, origin, scale, userPosition) => {
		let adjustedUserPosition = {
			x: (userPosition.x - origin.x) / scale.x,
			z: (userPosition.z - origin.z) / scale.z
		};
		let newPosition = {x: Math.random(), z: Math.random()};
		while (_distance(newPosition, adjustedUserPosition) < DIST_AWAY_PLAYER) {
			newPosition = {x: Math.random(), z: Math.random()};
		}
		let positions = [newPosition];
		for (let i = 1; i < size; i++) {
			//Fall near first position generated above:
			positions.push(
				_add({
						x: Math.random() * CLUMP_RADIUS, 
						z: Math.random() * CLUMP_RADIUS
					}, 
					positions[0]
				)
			);
		}
		for (let i = 0; i < size; i++) {
			positions[i] = _fix(positions[i], origin, scale);
		}
		return positions;
	},
	generatePoisson: (p_mean, n_partitions, ids, origin, scale) => {
		let final = {};
		const poissonCallback = random.poisson(lambda = p_mean);
		for (let id of ids) {
			final[id] = [];
		}
		let idArray = getIdArray(ids, n_partitions);
		let dim = Math.sqrt(n_partitions);
		for (let i = 0; i < dim; i++) {
			for (let j = 0; j < dim; j++) {
				let currId = idArray.pop();
				//let numCoins = Math.round(poisson.sample(p_mean));
				let numCoins = 2 * poissonCallback();
				for (let idx = 0; idx < numCoins; idx++) {
					final[currId].push(_fix({
						x: (Math.random() / dim) + i / dim,
						z: (Math.random() / dim) + j / dim,
					}, origin, scale));
				}
			}
		}
		return final;
	},
	generateCorrelatedRandom: (amount_per, ids, origin, scale) => {
		return rescale(get2DCorrelatedVectors(amount_per, ids), origin, scale);
	},
	generateSingleCorrelated: (coins, id, index, origin, scale) => {
		if (Math.random() < PROB_ATTRACTED) {
			let picked_position;
			while (!picked_position) {
				picked_position = coins[id][getRandomInt(0, coins[id].length - 1)];
			}
			return _add(picked_position, {
				x: Math.random() * CLUMP_RADIUS * scale.x,
				y: 0,
				z: Math.random() * CLUMP_RADIUS * scale.z
			});
		} else {
			return _fix({
				x: Math.random(),
				z: Math.random()
			}, origin, scale);
		}
	}
};


/*
 * Returns array [id1, id3, id3, id2, id1, ...],
 * where size of array is number of partitions, and each id
 * gets equal representation (with rounding, random shuffling).
 */
const getIdArray = (ids, n_partitions) => {
	let rep = Math.floor(n_partitions / ids.length);
	let remainder = n_partitions - (rep * ids.length);
	let each_rep = {};
	for (let id of ids) {
		each_rep[id] = rep;
	}
	for (let i = 0; i < remainder; i++) {
		each_rep[ids[i]]++;
	}
	let ret = [];
	for (let i = 0; i < n_partitions; i++) {
		let valid_ids = Object.keys(each_rep);
		let picked_id = valid_ids[getRandomInt(0, valid_ids.length - 1)];
		ret.push(picked_id);
		if (--each_rep[picked_id] == 0) {
			delete each_rep[picked_id];
		}
	}
	return ret;
}

const get2DCorrelatedVectors = (amount_per, ids) => {
	let final = {};
	for (let id of ids) {
		final[id] = [];
		for (let i = 0; i < amount_per; i++) {
			if (i > 0 && Math.random() <= PROB_ATTRACTED) {
				// Pick random coin position:
				const picked_position = final[id][getRandomInt(0, final[id].length - 1)];
				final[id].push(_add({
					x: Math.random() * CLUMP_RADIUS,
					z: Math.random() * CLUMP_RADIUS
				}, picked_position));
			} else {
				// Pick uniform random position:
				final[id].push({ 
					x: Math.random(),
					z: Math.random() 
				});
			}
		}
	}
	return final;
}

// Returns x, y positions of amount_per coins
const get2DClumpVectors = (amount_per, num_clumps, ids) => {
	let final = {};
	for (let id of ids) {
		final[id] = [];
	}
	//for (let i = 0; i < amount_per; i++) {
		//Cycle through generating coin for ids[0], ids[1], etc:
		/*
		for (let id of ids) {
			let newPosition = { x: Math.random(), z: Math.random() };
			for (let existingCoin of final[id]) {
				//Exert gravitational force:
				let dist = _distance(existingCoin, newPosition);
				if (dist >= MIN_DIST) {
					let force = _gravity(dist, _unitVec(_subtract(existingCoin, newPosition)));
					newPosition = _add(newPosition, force);
					
					let newExistingCoin = _subtract(newPosition, force);
					existingCoin.x = newExistingCoin.x;
					existingCoin.z = newExistingCoin.z;
				}
			}
			final[id].push(newPosition);
			//...
			// Pull new coin in direction of existing coins of id's color:
		}
		*/
	for (let id of ids) {
		for (let i = 0; i < amount_per; i++) {
			let newPosition;
			if (i < num_clumps) {
				newPosition = { x: Math.random(), z: Math.random() };
			} else {
				//Randomly choose clump (indices 0 to num_clumps - 1):
				let index = getRandomInt(0, num_clumps - 1);
				//let adjustedMinDist = MIN_DIST / scale.x;
				newPosition = _add(
					final[id][index], 
					{x: Math.random() * CLUMP_RADIUS, z: Math.random() * CLUMP_RADIUS }
				);
			}
			final[id].push(newPosition);
		}
	}
	return final;
}

function getRandomInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
}
/*
 * Resizes 2d vectors (ranging from 0 to 1), based on origin of map 
 * (3d vector) and scale vector. Returns 3d vectors:
 */
const rescale = (vectors, origin, scale) => {
	let newVectors = {};
	for (let id of Object.keys(vectors)) {
		newVectors[id] = [];
		for (let vec of vectors[id]) {
			newVectors[id].push(_fix(vec, origin, scale));
		}
	}
	return newVectors;
}

const _subtract = (to, from) => {
	return {
		x: to.x - from.x,
		z: to.z - from.z
	};
}

const _unitVec = (v) => {
	let mag = _magnitude(v);
	return {
		x: v.x / mag,
		z: v.z / mag
	};
}

const _add = (v1, v2) => {
	return {
		x: v1.x + v2.x,
		z: v1.z + v2.z
	};
}


// Fixes single vector {x, z}, returns 3d vector
const _fix = (vec, origin, scale) => {
	return {
		x: origin.x + scale.x * vec.x,
		y: origin.y,
		z: origin.z + scale.z * vec.z
	};
}

const _distance = (v1, v2) => {
	return Math.sqrt(Math.pow(v1.x - v2.x, 2) + Math.pow(v1.z - v2.z, 2));
}

const _magnitude = (v) => {
	return Math.sqrt(Math.pow(v.x, 2) + Math.pow(v.z, 2));
}

/*
const _gravity = (distance, direction) => {
	let f = GRAVITY_STRENGTH / (distance * distance);
	return {
		x: direction.x * f,
		z: direction.z * f,
	}
}
*/
