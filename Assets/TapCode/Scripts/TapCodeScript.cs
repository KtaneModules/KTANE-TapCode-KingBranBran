using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;
using Array = System.Array;

public class TapCodeScript : MonoBehaviour {

    public new KMAudio audio;
    public KMBombModule module;
    public KMSelectable button;
    public KMBombInfo bombInfo;

    private string[] words;
    private string[] letterTaps;
    private int[] submission;
    private string submissionString;
    private int wordIndex;
    private string serialNum;
    string chosenWord;
    string editedWord;
    string newWord;
	bool activated;
    bool playCoro;
    bool paused;
    bool holding;
    bool modulepass = false;
    int pressCount = -1;
    int stage = 0;
    bool coorotineStart;
    Coroutine releaseCoroutine = null;

    private static int _moduleIdCounter = 1;
    private int _moduleId;



    void Start () {

        words = new string[100] { "child", "style", "shake", "alive", "axion", "wreck", "cause", "pupil", "cheat", "watch",
                                  "jelly", "clock", "quark", "grass", "laser", "jeans", "yacht", "rumor", "fault", "hover",
                                  "sheet", "aware", "shell", "jolly", "giant", "vague", "image", "acute", "arena", "visit",
                                  "table", "force", "chair", "quick", "suite", "large", "chord", "power", "aloof", "attic",
                                  "cover", "prize", "trail", "cycle", "sight", "zeros", "glare", "angle", "ranch", "upset",
                                  "mixer", "drive", "xenon", "water", "judge", "right", "sweet", "gloom", "clash", "abbey",
                                  "level", "quilt", "climb", "tease", "knock", "fairy", "queen", "zebra", "guide", "south",
                                  "funny", "proud", "young", "jumpy", "staff", "query", "trunk", "zooms", "smart", "ghost",
                                  "judge", "yield", "brain", "helix", "small", "noise", "seize", "robot", "stain", "where",
                                  "world", "shark", "beard", "disco", "yummy", "title", "story", "color", "short", "fresh" };

        letterTaps = new string[25] { "A", "B", "C", "D", "E",
                                      "F", "G", "H", "I", "J",
                                      "L", "M", "N", "O", "P",
                                      "Q", "R", "S", "T", "U",
                                      "V", "W", "X", "Y", "Z"};

        submission = new int[10];
        paused = true;
        submissionString = "";
        serialNum = KMBombInfoExtensions.GetSerialNumber(bombInfo);

        module.OnActivate += Activate;
        button.OnInteract += delegate { ButtonPressed(); return false; };
        button.OnInteractEnded += ButtonReleased;
    }
	
    private void Activate()
    {
        _moduleId = _moduleIdCounter++;

        chosenWord = SelectWord();
        DebugLog("Selected word: {0} => {1}", chosenWord.ToUpperInvariant(), FindEditedWord(chosenWord).ToUpperInvariant());

        newWord = FindNewWord();
        editedWord = FindEditedWord(newWord);
        DebugLog("Correct word: {0} => {1}", newWord.ToUpperInvariant(), editedWord.ToUpperInvariant());

	    activated = true;
    }

    private string SelectWord()
    {
        wordIndex = Random.Range(0, words.Length);
        return words[wordIndex];
    }

    private string FindNewWord()
    {
        string finalWord;

        string firstTwoSN = serialNum.Substring(0, 2);
        string cond = FindNumLetCond(firstTwoSN);

        string direction;
        if (cond[0] == 'X')
        {
            direction = cond[1] == 'X' ? "up" : "left";
        }
        else
        {
            direction = cond[1] == 'X' ? "right" : "down";
        }

        int amount;

        if (bombInfo.GetSerialNumber().Contains("0"))
        {
            amount = bombInfo.GetSerialNumberNumbers().Sum();
        }
        else
        {
            int.TryParse(serialNum[5].ToString(), out amount);
        }

        int x = wordIndex % 10;
        int y = wordIndex / 10;

        finalWord = LookForWord(direction, x, y, amount);

        return finalWord;
    }

    private string LookForWord(string direction, int x, int y, int amount)
    {
        int nX = x;
        int nY = y;

        switch(direction)
        {
            case "up":
                nY = ((nY - amount) % 10 + 10) % 10;
                break;
            case "down":
                nY = (nY + amount) % 10;
                break;
            case "left":
                nX = ((nX - amount) % 10 + 10) % 10;
                break;
            default: // Right
                nX = (nX + amount) % 10;
                break;
        }

        return words[nX + nY * 10];
    }

    private string FindNumLetCond(string chars)
    {
        string cond = "";
        char charOne = serialNum[0];
        char charTwo = serialNum[1];

        if ('A' <= charOne && charOne <= 'Z')
        {
            cond += 'A' <= charTwo && charTwo <= 'Z' ? "XX" : "X#";
        }
        else
        {
            cond += 'A' <= charTwo && charTwo <= 'Z' ? "#X" : "##";
        }

        return cond;
    }

    private void ButtonPressed()
    {
	    if (releaseCoroutine != null)
	    {
		    StopCoroutine(releaseCoroutine);
	    }

		if (playCoro || modulepass) return;
	    if (!activated)
	    {
		    return;
	    }
        holding = true;
        StartCoroutine(StartButtonHoldTimer());
    }

    private void ButtonReleased()
    {
	    if (!holding || !activated) return;
        if (modulepass) return;
        holding = false;
        if (playCoro) return;
        if (paused)
        {
            if (pressCount != -1)
            {
                submission[stage] = pressCount;
                stage++;
                if (stage == 10)
                {
                    CheckForCorrect();
                    return;
                }
            }
            pressCount = 0;
            paused = false;
        }
        pressCount++;

        releaseCoroutine = StartCoroutine(StartButtonReleaseTimer());
    }

    private void CheckForCorrect()
    {
        int chars = 0;

        for (int c = 0; c < 5; c++)
        {
            int y = submission[chars] - 1;
            int x = submission[chars + 1] - 1;
            int index = x + y * 5;

            submissionString += x > 4 || y > 4 ? "?" : letterTaps[index];
            chars += 2;
        }
        if (submissionString == editedWord.ToUpperInvariant())
        {
            DebugLog("You Submitted {0}. Thats Correct!", submissionString);
            DebugLog("Module Passed!");
            module.HandlePass();
            modulepass = true;
        }
        else
        {
            DebugLog("You Submitted {0}. Strike!", submissionString);
            module.HandleStrike();
            ResetEntry();
        }
    }

    void ResetEntry()
    {
        stage = 0;
        pressCount = -1;
        submissionString = "";
        submission = new int[10];
    }

    string FindEditedWord(string w)
    {
        string word = "";
        foreach (char c in w)
        {
            if (c == 'k')
            {
                word += "c";
            }
            else
            {
                word += c;
            }
        }
        return word;
    }

    private IEnumerator StartButtonHoldTimer()
    {

        double t = 0;
        while (t < 1 && holding)
        {
            yield return new WaitForSeconds(0.1f);
            t += 0.1;
        }

        if (holding)
        {
            // Play Start Sound Here
            playCoro = true;
            ResetEntry();
            StartCoroutine(PlayWord());
        }
    }

    private IEnumerator PlayWord()
    {
        foreach (char c in chosenWord)
        {
            int index = Array.IndexOf(letterTaps, c.ToString().ToUpperInvariant());
            if (index == -1) index = 2; // This is because K = C
            int x = index % 5 + 1; // x and y are 1 based here.
            int y = index / 5 + 1;

            for (int a = 0; a < y; a++)
            {
                audio.PlaySoundAtTransform("Tap", module.transform);
                yield return new WaitForSeconds(.5f);
            }
            yield return new WaitForSeconds(.5f);
            

            for (int a = 0; a < x; a++)
            {
                audio.PlaySoundAtTransform("Tap", module.transform);
                yield return new WaitForSeconds(.5f);
            }
            yield return new WaitForSeconds(.5f);
            
        }
        playCoro = false;
    }

    private IEnumerator StartButtonReleaseTimer()
    {
        double t = 0;
        while (t < 1)
        {
            yield return new WaitForSeconds(0.1f);
            t += 0.1;
        }
        paused = true;
        audio.PlaySoundAtTransform("MiniTap", module.transform);
    }

    private void ModuleSelected()
    {
        audio.PlaySoundAtTransform("Tap", module.transform);
    }

    private void DebugLog(string log, params object[] args)
    {
        var logData = string.Format(log, args);
        Debug.LogFormat("[Tap Code #{0}] {1}", _moduleId, logData);
    }

#pragma warning disable 414
	private string TwitchHelpMessage = "Listen to the taps with !{0} listen.  Tap your answer with !{0} tap 24 32 11 22 15";
#pragma warning restore 414

	private IEnumerator ProcessTwitchCommand(string command)
	{
		if (!Regex.IsMatch(command, "^(?:(?:listen|play)|(?:tap |submit |press |)(?:[1-5][ ,;]*){10})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)) yield break;
		DebugLog("Command is: ", command);
		command = command.ToLowerInvariant();

		yield return null;

		while (playCoro)
		{
			yield return "trycancel";
		}

		if (command == "listen" || command == "play")
		{
			yield return button;
			yield return new WaitUntil(() => playCoro);
			yield return new WaitForSeconds(0.1f);
			yield return button;
			while (playCoro)
			{
				yield return "trywaitcancel 0.1";
			}
			yield break;
		}

		ResetEntry();
		foreach (char tap in command)
		{
			yield return "trycancel";

			int taps;
			if (!int.TryParse(tap.ToString(), out taps)) continue;
			for (int i = 0; i < taps; i++)
			{
				yield return button;
				yield return new WaitForSeconds(0.05f);
				yield return button;
				yield return new WaitForSeconds(0.05f);
				yield return "trycancel";
			}

			yield return new WaitUntil(() => paused);
			yield return new WaitForSeconds(0.1f);
		}

		yield return "trycancel";
		yield return button;
		yield return new WaitForSeconds(0.05f);
		yield return button;
		yield return new WaitForSeconds(0.05f);

	}
}
