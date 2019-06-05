//const MIN_DIST = 0.3; //Absolute coordinates (necessary due to abs size of coin prefab)
//const GRAVITY_STRENGTH = 0.02;
const CLUMP_RADIUS = 0.10; //0 to 1 scale

module.exports = {
	generateAll: (amount_per, num_clumps, ids, origin, scale) => {
		return rescale(get2DVectors(amount_per, num_clumps, ids), origin, scale);
	},
	generateClump: (size, origin, scale) => {
		let positions = [{x: Math.random(), z: Math.random()}];
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
	}
};

// Returns x, y positions of amount_per coins
const get2DVectors = (amount_per, num_clumps, ids) => {
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
