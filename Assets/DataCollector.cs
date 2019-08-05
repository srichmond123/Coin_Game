using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using UnityEngine;
using Directory = System.IO.Directory;

public class DataCollector : MonoBehaviour {
	private static string _path = "";
	private static StreamWriter _streamWriter, _movementWriter; //Diff writer for movement as it's every frame
	private static int _currRound = -1;
	private static int _streamFlushCounter = 0;
	void Start() {
		
	}

	void Update() {
		
	}

	public static void WriteMovement() {
		string writeTo = $"{_path}Round_{Interface.RoundNum}/Movement.csv";
		if (_currRound != Interface.RoundNum || _movementWriter == null) {
			_currRound = Interface.RoundNum;
			_movementWriter?.Flush();
			_movementWriter = new StreamWriter(writeTo, true);
			_writeWords(_movementWriter, new[] {
				"Date and clock time (yyyy/MM/dd - hh:mm:ss.ffffff)",
				"Server time (milliseconds)",
				"Game engine time (seconds)",	
				"My points",
				"Team points",
				"My brightness",
				"Blue brightness",
				"Red brightness",
				"Head x pos",
				"Head y pos",
				"Head z pos",
				"Head x rot",
				"Head y rot",
				"Head z rot",
				"Map x",
				"Map y",
				"Map z",
				"Right x pos",
				"Right y pos",
				"Right z pos",
				"Right x rot",
				"Right y rot",
				"Right z rot",
				"Left x pos",
				"Left y pos",
				"Left z pos",
				"Left x rot",
				"Left y rot",
				"Left z rot",
				"Swimming"	
			});
		}
		string[] line = new [] {
			DateTime.Now.ToString("yyyy/MM/dd - hh:mm:ss.fffffff"),
			(Interface._elapsedMs - Interface.CountdownTimeMs).ToString(),
			Interface._unityTime.ToString(),
		};

		line = Concat(line, _getStandard());
		
		_writeWords(_movementWriter, line);
	}

	public static void WriteEvent(string evt, string toWhom) {
		string writeTo = $"{_path}Round_{Interface.RoundNum}/Events.csv";
		if (!File.Exists(writeTo)) {
			_writeWords(writeTo, new[] {
				"Date and clock time (yyyy/MM/dd - hh:mm:ss.ffffff)",
				"Server time (milliseconds)",
				"Game engine time (seconds)",
				"Action", //collect, give, start swimming, or stop swimming
				"To whom", //color or ""
				"My points",
				"Team points",
				"My brightness",
				"Blue brightness",
				"Red brightness",
				"Head x pos",
				"Head y pos",
				"Head z pos",
				"Head x rot",
				"Head y rot",
				"Head z rot",
				"Map x",
				"Map y",
				"Map z",
				"Right x pos",
				"Right y pos",
				"Right z pos",
				"Right x rot",
				"Right y rot",
				"Right z rot",
				"Left x pos",
				"Left y pos",
				"Left z pos",
				"Left x rot",
				"Left y rot",
				"Left z rot",
				"Swimming"
			});
		}

		string[] line = new [] {
			DateTime.Now.ToString("yyyy/MM/dd - hh:mm:ss.fffffff"),
			(Interface._elapsedMs - Interface.CountdownTimeMs).ToString(),
			Interface._unityTime.ToString(),
			evt,
			toWhom,
		};

		line = Concat(line, _getStandard());
		
		_writeWords(writeTo, line);
	}

	public static string[] Concat(string[] a, string[] b) {
		string[] res = new string[a.Length + b.Length];
		int ind = 0;
		foreach (string v in a) {
			res[ind++] = v;
		}
		foreach (string v in b) {
			res[ind++] = v;
		}

		return res;
	}

	private static string[] _getStandard() { // Head pos, rot, left hand, right hand, etc:
		Vector3 headPosition = Interface.HeadPosition();
		Vector3 headRotation = Interface.GetMyRotation().eulerAngles;
		Vector3 mapPosition = Interface.GetMyPosition();

		Vector3 rightPosition = Interface.RightHandPosition();
		Vector3 rightRotation = Interface.RightHandRotation();
		Vector3 leftPosition = Interface.LeftHandPosition();
		Vector3 leftRotation = Interface.LeftHandRotation();

		return new[] {
			Interface.MyScore.ToString(), Interface.ScoreSum.ToString(), 
			Interface.light.range.ToString(),
			Interface.GetFriendByColor(Color.blue).GetRange().ToString(),
			Interface.GetFriendByColor(Color.red).GetRange().ToString(),
			headPosition.x.ToString(), headPosition.y.ToString(), headPosition.z.ToString(),
			headRotation.x.ToString(), headRotation.y.ToString(), headRotation.z.ToString(),
			mapPosition.x.ToString(), mapPosition.y.ToString(), mapPosition.z.ToString(),
			rightPosition.x.ToString(), rightPosition.y.ToString(), rightPosition.z.ToString(),
			rightRotation.x.ToString(), rightRotation.y.ToString(), rightRotation.z.ToString(),
			leftPosition.x.ToString(), leftPosition.y.ToString(), leftPosition.z.ToString(),
			leftRotation.x.ToString(), leftRotation.y.ToString(), leftRotation.z.ToString(),
			Interface.flying.ToString(),
		};
	}

	public static string ColorName(Color c) {
		if (Buckets.CompareRGB(c, Color.red)) {
			return "red";
		}

		if (Buckets.CompareRGB(c, Color.blue)) {
			return "blue";
		}
		
        return "white";
	}

	//User id, timestamp of start, topology, Goal, coins per, min range, range decrease, range increase;
	public static void WriteMetaData(int coinsPer) {
		if (!_path.Equals("")) { //Path Data/Game_X must have been set already:
			string writeTo = $"{_path}MetaData.csv";
			string[] head = new [] {
				"My ID", 
				"Date and clock time (yyyy/MM/dd - hh:mm:ss.ffffff)",
				"Blue ID",
				"Red ID",
				"Can send to",
				"Goal",
				"Coins per player",
				"Min range",
				"Range decrease per second",
				"Range increase",
			};
			_writeWords(writeTo, head);
			string permissibleColors = "";
			foreach (string id in Interface.permissibleIndividuals) {
				string colName = ColorName(Interface.GetFriendById(id).GetColor());
				if (!permissibleColors.Equals("")) permissibleColors += " and ";
				permissibleColors += colName;
			}
			string[] content = new [] {
				Interface.MyId,
				DateTime.Now.ToString("yyyy/MM/dd - hh:mm:ss.fffffff"),
				Interface.GetFriendByColor(Color.blue).GetId(),
				Interface.GetFriendByColor(Color.red).GetId(),
				permissibleColors,
				Interface.Goal.ToString(),
				coinsPer.ToString(),
				Interface.MinRange.ToString(),
				Interface.ConstDecrease.ToString(),
				Interface.OwnRangeIncrease.ToString(),
			};
			_writeWords(writeTo, content);
		}
	}

	private static void _writeWords(string filePath, string[] words) {
		_streamWriter = new StreamWriter(filePath, true);
		_streamWriter.Write(string.Join(",", words) + "\n");
		_streamWriter.Close();
	}

	private static void _writeWords(StreamWriter sw, string[] words) {
		sw.Write(string.Join(",", words) + "\n");
		if (++_streamFlushCounter >= 60) {
			sw.Flush();
			_streamFlushCounter = 0;
		}
	}

	public static void SetPath(int gameNum) {
		_path = (Interface.Release ? (Application.persistentDataPath + "/") : "") + "Data/";
		if (!Directory.Exists(_path)) { //If first game ever:
			Directory.CreateDirectory(_path);
		}

		_path += "Game_" + gameNum;
		string append = "";
		int ind = 1;
		while (Directory.Exists(_path + append)) { //Ensure fresh directory for data stream:
			append = " (" + (ind++) + ")";
		}

		_path += append + "/";
		Directory.CreateDirectory(_path);

		for (int i = 1; i <= 3; i++) {
			Directory.CreateDirectory(_path + "Round_" + i);
		}
	}

	public static void FlushAll() {
		_movementWriter?.Flush();
	}
}
