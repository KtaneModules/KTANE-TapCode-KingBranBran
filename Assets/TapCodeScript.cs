using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TapCodeScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable Sel;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly string[] _wordList = new string[100] {
        "child", "style", "shake", "alive", "axion", "wreck", "cause", "pupil", "cheat", "watch",
        "jelly", "clock", "quark", "grass", "laser", "jeans", "yacht", "rumor", "fault", "hover",
        "sheet", "aware", "shell", "jolly", "giant", "vague", "image", "acute", "arena", "visit",
        "table", "force", "chair", "quick", "suite", "large", "chord", "power", "aloof", "attic",
        "cover", "prize", "trail", "cycle", "sight", "zeros", "glare", "angle", "ranch", "upset",
        "mixer", "drive", "xenon", "water", "venom", "right", "sweet", "gloom", "clash", "abbey",
        "level", "quilt", "climb", "tease", "knock", "fairy", "queen", "zebra", "guide", "south",
        "funny", "proud", "young", "jumpy", "staff", "query", "trunk", "zooms", "smart", "ghost",
        "judge", "yield", "brain", "helix", "small", "noise", "seize", "robot", "stain", "where",
        "world", "shark", "beard", "disco", "yummy", "title", "story", "color", "short", "fresh"
    };

    private static readonly string[] _convertedWordList = new string[100]
    {
        "child", "style", "shace", "alive", "axion", "wrecc", "cause", "pupil", "cheat", "watch",
        "jelly", "clocc", "quarc", "grass", "laser", "jeans", "yacht", "rumor", "fault", "hover",
        "sheet", "aware", "shell", "jolly", "giant", "vague", "image", "acute", "arena", "visit",
        "table", "force", "chair", "quicc", "suite", "large", "chord", "power", "aloof", "attic",
        "cover", "prize", "trail", "cycle", "sight", "zeros", "glare", "angle", "ranch", "upset",
        "mixer", "drive", "xenon", "water", "venom", "right", "sweet", "gloom", "clash", "abbey",
        "level", "quilt", "climb", "tease", "cnocc", "fairy", "queen", "zebra", "guide", "south",
        "funny", "proud", "young", "jumpy", "staff", "query", "trunc", "zooms", "smart", "ghost",
        "judge", "yield", "brain", "helix", "small", "noise", "seize", "robot", "stain", "where",
        "world", "sharc", "beard", "disco", "yummy", "title", "story", "color", "short", "fresh"
    };

    private string _chosenWord;
    private string _solutionWord;

    private int X;
    private int Y;

    private List<int> _chosenTapCode = new List<int>();
    private List<int> _solutionTapCode = new List<int>();
    private List<int> _tapCodeInput = new List<int>();

    private bool _tapCodeActive;
    private Coroutine _waitingToInput;
    private Coroutine _timer;
    private float _elapsedTime;
    private Coroutine _playTapCode;
    private bool _finalTap;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Sel.OnInteract += SelPress;
        Sel.OnInteractEnded += SelRelease;

        var SerialNumber = BombInfo.GetSerialNumber();
        var movementIx = SerialNumber[5] - '0';
        if (movementIx == 0)
            for (int i = 0; i < 6; i++)
                if (SerialNumber[i] - '0' >= 0 && SerialNumber[i] - '0' <= 9)
                    movementIx += SerialNumber[i] - '0';
        Debug.LogFormat("[Tap Code #{0}] Serial number digit: {1}", _moduleId, movementIx);

        var wordIx = Rnd.Range(0, 100);
        _chosenWord = _wordList[wordIx];

        X = wordIx % 10;
        Y = wordIx / 10;

        if (SerialNumber[0] >= '0' && SerialNumber[0] <= '9')
        {
            if (SerialNumber[1] >= '0' && SerialNumber[1] <= '9')
            {
                Debug.LogFormat("[Tap Code #{0}] Serial Number's first two characters are NUMBER NUMBER. Moving down.", _moduleId);
                Y = (Y + movementIx) % 10;
            }
            else
            {
                Debug.LogFormat("[Tap Code #{0}] Serial Number's first two characters are NUMBER LETTER. Moving right.", _moduleId);
                X = (X + movementIx) % 10;
            }
        }
        else
        {
            if (SerialNumber[1] >= '0' && SerialNumber[1] <= '9')
            {
                Debug.LogFormat("[Tap Code #{0}] Serial Number's first two characters are LETTER NUMBER. Moving left.", _moduleId);
                X = (X - movementIx + 10) % 10;
            }
            else
            {
                Debug.LogFormat("[Tap Code #{0}] Serial Number's first two characters are LETTER LETTER. Moving up.", _moduleId);
                Y = (Y - movementIx + 10) % 10;
            }
        }

        _solutionWord = _wordList[Y * 10 + X];

        var chosenConverted = _convertedWordList[Array.IndexOf(_wordList, _chosenWord)].ToUpperInvariant();
        var solutionConverted = _convertedWordList[Array.IndexOf(_wordList, _solutionWord)].ToUpperInvariant();

        for (int i = 0; i < 5; i++)
        {
            int tapY;
            int tapX;
            int chosenVal = chosenConverted[i] - 'A';
            if (chosenVal > 9)
                chosenVal--;
            int solutionVal = solutionConverted[i] - 'A';
            if (solutionVal > 9)
                solutionVal--;
            tapY = chosenVal / 5 + 1;
            tapX = chosenVal % 5 + 1;
            _chosenTapCode.Add(tapY);
            _chosenTapCode.Add(tapX);
            tapY = solutionVal / 5 + 1;
            tapX = solutionVal % 5 + 1;
            _solutionTapCode.Add(tapY);
            _solutionTapCode.Add(tapX);
        }
        var chosenChunks = new List<string>();
        for (var i = 0; i < _chosenTapCode.Count; i += 2)
            chosenChunks.Add(_chosenTapCode.Skip(i).Take(2).Join(""));
        var solutionChunks = new List<string>();
        for (var i = 0; i < _solutionTapCode.Count; i += 2)
            solutionChunks.Add(_solutionTapCode.Skip(i).Take(2).Join(""));

        Debug.LogFormat("[Tap Code #{0}] Chosen word: {1}", _moduleId, _chosenWord.ToUpperInvariant());
        Debug.LogFormat("[Tap Code #{0}] Solution word: {1}", _moduleId, _solutionWord.ToUpperInvariant());
        Debug.LogFormat("[Tap Code #{0}] Chosen word to Tap Code: {1}", _moduleId, chosenChunks.Join(" "), _chosenTapCode.Skip(2 * (_chosenTapCode.Count / 2)).Join(""));
        Debug.LogFormat("[Tap Code #{0}] Solution word to Tap Code: {1}", _moduleId, solutionChunks.Join(" "), _solutionTapCode.Skip(2 * (_solutionTapCode.Count / 2)).Join(""));
    }

    private bool SelPress()
    {
        if (_moduleSolved)
            return false;
        if (_timer != null)
            StopCoroutine(_timer);
        _timer = StartCoroutine(Timer());
        return false;
    }

    private void SelRelease()
    {
        if (_moduleSolved)
            return;
        if (_timer != null)
            StopCoroutine(_timer);
        if (_elapsedTime < 0.5f)
        {
            if (_finalTap)
            {
                interpretTapCode();
                return;
            }
            if (_playTapCode != null)
                StopCoroutine(_playTapCode);
            if (_tapCodeActive)
                _tapCodeInput[_tapCodeInput.Count - 1]++;
            else
            {
                _tapCodeActive = true;
                _tapCodeInput.Add(1);
            }
            if (_waitingToInput != null)
                StopCoroutine(_waitingToInput);
            _waitingToInput = StartCoroutine(acknowledgeTapCode());
        }
        else
        {
            _finalTap = false;
            _tapCodeInput.Clear();
        }
    }

    private IEnumerator Timer()
    {
        _elapsedTime = 0f;
        while (_elapsedTime < 0.5f)
        {
            yield return null;
            _elapsedTime += Time.deltaTime;
        }
        _playTapCode = StartCoroutine(PlayTapCode());
    }

    private IEnumerator PlayTapCode()
    {
        for (int i = 0; i < _chosenTapCode.Count; i++)
        {
            for (int j = 0; j < _chosenTapCode[i]; j++)
            {
                Audio.PlaySoundAtTransform("Tap", transform);
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator acknowledgeTapCode()
    {
        yield return new WaitForSeconds(1f);
        Audio.PlaySoundAtTransform("MiniTap", transform);
        _tapCodeActive = false;
        if (_tapCodeInput.Count < 10)
            yield break;
        _finalTap = true;
        //interpretTapCode();
    }

    private void interpretTapCode()
    {
        if (_waitingToInput != null)
        {
            StopCoroutine(_waitingToInput);
            _waitingToInput = null;
        }

        if (_solutionTapCode.SequenceEqual(_tapCodeInput))
        {
            _moduleSolved = true;
            Module.HandlePass();
            Debug.LogFormat("[Tap Code #{0}] Submitted {1}. Module solved.", _moduleId, _solutionWord);
        }
        else
        {
            Module.HandleStrike();
            var chunks = new List<string>();
            for (var i = 0; i < _tapCodeInput.Count; i += 2)
                chunks.Add(_tapCodeInput.Skip(i).Take(2).Join(""));
            Debug.LogFormat("[Tap Code #{0}] Inputted {1} {2}. Strike.", _moduleId, chunks.Join(" "), _tapCodeInput.Skip(2 * (_tapCodeInput.Count / 2)).Join(""));
        }
        _finalTap = false;
        _tapCodeInput.Clear();
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "!{0} tap 11 22 33 44 55 | !{0} listen";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, "^(?:(?<L>listen|play)|(?:tap |submit |press |)(?<D>([1-5][ ,;]*){10}))$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (!m.Success)
            yield break;
        yield return null;
        if (m.Groups["L"].Success)
        {
            Sel.OnInteract();
            yield return new WaitForSeconds(0.7f);
            Sel.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        foreach (char tap in m.Groups["D"].Value)
        {
            yield return "trycancel";
            int taps;
            if (!int.TryParse(tap.ToString(), out taps))
                continue;
            for (int i = 0; i < taps; i++)
            {
                Sel.OnInteract();
                yield return new WaitForSeconds(0.05f);
                Sel.OnInteractEnded();
                yield return new WaitForSeconds(0.05f);
                yield return "trycancel";
            }
            while (_tapCodeActive)
                yield return "trycancel";
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.05f);
        Sel.OnInteract();
        yield return new WaitForSeconds(0.05f);
        Sel.OnInteractEnded();
        yield return new WaitForSeconds(0.05f);
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (_tapCodeActive)
            yield return true;
        bool fineSoFar = true;
        int amountCorrect = 0;
        for (int i = 0; i < _tapCodeInput.Count; i++)
        {
            if (_tapCodeInput[i] != _solutionTapCode[i])
            {
                fineSoFar = false;
                break;
            }
            amountCorrect++;
        }
        if (!fineSoFar)
        {
            amountCorrect = 0;
            Sel.OnInteract();
            yield return new WaitForSeconds(0.7f);
            Sel.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = amountCorrect; i < _solutionTapCode.Count; i++)
        {
            for (int j = 0; j < _solutionTapCode[i]; j++)
            {
                Sel.OnInteract();
                yield return new WaitForSeconds(0.1f);
                Sel.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
            }
            while (_tapCodeActive)
                yield return true;
        }
        yield return new WaitForSeconds(0.1f);
        Sel.OnInteract();
        yield return new WaitForSeconds(0.1f);
        Sel.OnInteractEnded();
        yield return new WaitForSeconds(0.1f);
        while (!_moduleSolved)
            yield return true;
    }
}
