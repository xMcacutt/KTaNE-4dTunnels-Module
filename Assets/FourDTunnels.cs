using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class FourDTunnels : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable ButtonUp;
    public KMSelectable ButtonDown;
    public KMSelectable ButtonLeft;
    public KMSelectable ButtonRight;
    public KMSelectable ButtonZig;
    public KMSelectable ButtonZag;
    public KMSelectable ButtonTarget;
    public GameObject ZagIndicator;
    public GameObject ZigIndicator;
    public GameObject Display;
    public GameObject ZagDisplay;
    public GameObject ZigDisplay;
    public TextMesh Symbol;
    public TextMesh TargetSymbol;
    public Material IndicatorOff;
    public Material IndicatorOn;

    private static readonly string _symbols = "" +
                                              "" +
                                              "";
    private static readonly string[] _symbolNames = {
        "Snowflake", "Alarm", "Animation", "Law", "Lightning", "Bomb", "Book", "Bookmark", "Shutter",
        "Triangle", "Hanger", "Pawn", "Circle", "Cookie", "Crop", "Corners", "Crown", "Cyclone",
        "Database", "Diamond", "Server", "Leaf", "Compass", "Sigma", "Grid", "Cross", "Hexagon",
        
        "Chip", "Ring", "Drop", "Cube", "Cloud", "Command", "Heart monitor", "Anchor", "Medal",
        "Lock", "Crossing", "Moon", "Globe", "Heart", "Link", "Eye", "Feather", "Flag",
        "Chart", "Umbrella", "Wind", "Shield", "Star", "Sun", "Quarter", "Radio", "Gear", 
        
        "Trophy", "Plane", "Target", "Ball", "Omega", "Send", "Tag", "Science", "School",
        "Rocket", "Power", "Planet", "Pentagon", "Palette", "Brain", "Arrow", "Reticule", "Note",
        "Fire", "Fan", "Map", "Cocktail", "Bulb", "Key", "Pin", "Pen", "Hourglass"
    };
    
    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private Location _location;
    private ShipDirection _direction;
    private HashSet<int> _identifiedNodes;
    private List<int> _targetNodes;
    private int _numIdentifiedNodes = 18;
    private int _numTargetNodes = 3;
    private int _currentTarget = 0;
    private bool _solved = false;
    private List<Action> _actionLog = new List<Action>();
    private enum Button { Up, Right, Down, Left, Zig, Zag, Target };
    private enum StrikeReason { None, FlyIntoWall, NotOnTarget };

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        ButtonUp.OnInteract = () => { PressButton(Button.Up); return false; };
        ButtonDown.OnInteract = () => { PressButton(Button.Down); return false; };
        ButtonRight.OnInteract = () => { PressButton(Button.Right); return false; };
        ButtonLeft.OnInteract = () => { PressButton(Button.Left); return false; };
        ButtonZig.OnInteract = () => { PressButton(Button.Zig); return false; };
        ButtonZag.OnInteract = () => { PressButton(Button.Zag); return false; };
        ButtonTarget.OnInteract = () => { PressTargetButton(); return false; };

        var found = false;
        while (!found)
        {
            // Random identified nodes (3x3x3x3 grid = 81 nodes)
            _identifiedNodes = new HashSet<int>();
            while (_identifiedNodes.Count < _numIdentifiedNodes)
                _identifiedNodes.Add(Rnd.Range(0, 81));

            // Check if there is at least a pair that's in the same 3D hyperplane
            foreach (var node1 in _identifiedNodes)
            {
                foreach (var node2 in _identifiedNodes)
                {
                    if (node1 == node2) continue;
                    if (node1 == 40 || node2 == 40) continue; // Middle node (1,1,1,1) = 40
                    var loc1 = LocationFromIndex(node1);
                    var loc2 = LocationFromIndex(node2);
                    var distances = new List<int> { Math.Abs(loc1.X - loc2.X), Math.Abs(loc1.Y - loc2.Y), Math.Abs(loc1.Z - loc2.Z), Math.Abs(loc1.W - loc2.W) };
                    // Nodes are adjacent if exactly one dimension differs by 1, others are 0
                    if (distances.Count(d => d == 0) == 3 && distances.Count(d => d == 1) == 1)
                        found = true;
                    // Optionally, allow nodes differing by 1 in two dimensions, others 0
                    if (distances.Count(d => d == 0) == 2 && distances.Count(d => d == 1) == 2)
                        found = true;
                    if (found) break;
                }
                if (found) break;
            }
        }
        Debug.LogFormat("[4D Tunnels #{0}] Identified nodes: {1}", _moduleId, String.Join(", ", _identifiedNodes.Select(x => _symbolNames[x]).ToArray()));

        // Random target nodes, excluding center node and identified nodes
        var targetNodes = new HashSet<int>(_identifiedNodes);
        while (targetNodes.Count < (_numIdentifiedNodes + _numTargetNodes))
        {
            var rnd = Rnd.Range(0, 81);
            if (rnd != 40) targetNodes.Add(rnd); // Exclude middle node (1,1,1,1)
        }
        _targetNodes = targetNodes.Except(_identifiedNodes).ToList();
        Debug.LogFormat("[4D Tunnels #{0}] Target nodes: {1}", _moduleId, String.Join(", ", _targetNodes.Select(x => _symbolNames[x]).ToArray()));

        // Random starting location
        do _location = new Location(Rnd.Range(0, 3), Rnd.Range(0, 3), Rnd.Range(0, 3), Rnd.Range(0, 3));
        while (_identifiedNodes.Contains(_location.ToInt()));

        // Random starting direction
        _direction = new ShipDirection();
        for (var i = 0; i < 4; i++)
            _direction.Rotate(new Axis((Dimension)Rnd.Range(0, 4), Rnd.Range(0, 2) == 1 ? -1 : 1), Rnd.Range(0, 2) == 1 ? -1 : 1);

        Debug.LogFormat("[4D Tunnels #{0}] Starting at {1}. {2}", _moduleId, _symbolNames[_location.ToInt()], GetOrientationDescription(_location, _direction));

        UpdateDisplay();
        StartCoroutine(RotateSymbol());
        StartCoroutine(ScaleSymbol());
    }
    
    private Location LocationFromIndex(int index)
    {
        var w = index % 3;
        index /= 3;
        var z = index % 3;
        index /= 3;
        var y = index % 3;
        var x = index / 3;
        return new Location(x, y, z, w);
    }

    private void UpdateDisplay()
    {
        // Update tunnels and walls for all directions
        var isWallFront = _direction.IsWall(_direction.Forwards, _location);
        Display.transform.Find("forward-tunnel").gameObject.SetActive(!isWallFront);
        Display.transform.Find("forward-wall").gameObject.SetActive(isWallFront);
        
        var isWallLeft = _direction.IsWall(new Axis(_direction.Right.Dimension, -_direction.Right.Sign), _location);
        Display.transform.Find("left-tunnel").gameObject.SetActive(!isWallLeft);
        Display.transform.Find("left-wall").gameObject.SetActive(isWallLeft);
        var isWallRight = _direction.IsWall(new Axis(_direction.Right.Dimension, _direction.Right.Sign), _location);
        Display.transform.Find("right-tunnel").gameObject.SetActive(!isWallRight);
        Display.transform.Find("right-wall").gameObject.SetActive(isWallRight);
        
        var isWallUp = _direction.IsWall(new Axis(_direction.Up.Dimension, _direction.Up.Sign), _location);
        Display.transform.Find("up-tunnel").gameObject.SetActive(!isWallUp);
        Display.transform.Find("up-wall").gameObject.SetActive(isWallUp);
        var isWallDown = _direction.IsWall(new Axis(_direction.Up.Dimension, -_direction.Up.Sign), _location);
        Display.transform.Find("down-tunnel").gameObject.SetActive(!isWallDown);
        Display.transform.Find("down-wall").gameObject.SetActive(isWallDown);
        
        var isWallZag = _direction.IsWall(new Axis(_direction.Zag.Dimension, _direction.Zag.Sign), _location);
        ZagDisplay.transform.Find("forward-tunnel").gameObject.SetActive(!isWallZag);
        ZagDisplay.transform.Find("forward-wall").gameObject.SetActive(isWallZag);
        ZagIndicator.GetComponent<MeshRenderer>().material = isWallZag ? IndicatorOn : IndicatorOff;
        var isWallZig = _direction.IsWall(new Axis(_direction.Zag.Dimension, -_direction.Zag.Sign), _location);
        ZigDisplay.transform.Find("forward-tunnel").gameObject.SetActive(!isWallZig);
        ZigDisplay.transform.Find("forward-wall").gameObject.SetActive(isWallZig);
        
        ZigIndicator.GetComponent<MeshRenderer>().material = isWallZig ? IndicatorOn : IndicatorOff;
        
        Symbol.gameObject.SetActive(_identifiedNodes.Contains(_location.ToInt()));
        Symbol.text = _symbols[_location.ToInt()].ToString();
        TargetSymbol.text = _symbols[_targetNodes[_currentTarget]].ToString();
    }

    private void PressButton(Button button)
    {
        if (_solved) return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        Action action = new Action() { StartLocation = _location, StartOrientation = _direction, Button = button };
        
        // Log move
        if (_identifiedNodes.Contains(_location.ToInt()))
        {
            _actionLog.Clear();
            action.LocationIsIdentified = true;
        }

        var direction = new  ShipDirection(_direction);
        
        // Turn
        switch (button)
        {
            case Button.Left:
                direction.Rotate(new Axis(direction.Right.Dimension, -direction.Right.Sign), -1);
                break;
            case Button.Right:
                direction.Rotate(new Axis(direction.Right.Dimension, direction.Right.Sign), 1);
                break;
            case Button.Up:
                direction.Rotate(new Axis(direction.Up.Dimension, direction.Up.Sign), 1);
                break;
            case Button.Down:
                direction.Rotate(new Axis(direction.Up.Dimension, -direction.Up.Sign), -1);
                break;
            case Button.Zig:
                direction.Rotate(new Axis(direction.Zag.Dimension, -direction.Zag.Sign), -1);
                break;
            case Button.Zag:
                direction.Rotate(new Axis(direction.Zag.Dimension, direction.Zag.Sign), 1);
                break;
        }
        action.EndOrientation = direction;

        // Check if you can move forward
        if (!direction.IsWall(direction.Forwards, _location))
        {
            // If so, move forward
            Debug.Log($"{(direction.Forwards.Sign == -1 ? "-" : "+")}{direction.Forwards.Dimension}");
            _direction = direction;
            _direction.MoveForward(ref _location);
            action.EndLocation = _location;
        }
        else
        {
            // Else, give a strike
            Module.HandleStrike();
            action.StrikeReason = StrikeReason.FlyIntoWall;
        }

        _actionLog.Add(action);
        if (action.StrikeReason != StrikeReason.None) LogActions();
        UpdateDisplay();
    }

    private void PressTargetButton()
    {
        if (_solved) return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        Action action = new Action() { StartLocation = _location, StartOrientation = _direction, Button = Button.Target };

        // Check if current location matches current target
        if (_location.ToInt() == _targetNodes[_currentTarget])
        {
            // If so, go to next stage, or module solved if this was the last stage
            Debug.LogFormat("[3d Tunnels #{0}] {1} identified correctly.", _moduleId, _symbolNames[_location.ToInt()]);
            if (_currentTarget == _numTargetNodes - 1)
            {
                Debug.LogFormat("[3d Tunnels #{0}] Module solved!", _moduleId);
                _solved = true;
                TargetSymbol.gameObject.SetActive(false);
                Module.HandlePass();
            }
            else
            {
                _identifiedNodes.Add(_location.ToInt());
                _currentTarget++;
            }
            UpdateDisplay();
        }
        else
        {
            // If not, give strike
            Module.HandleStrike();
            action.StrikeReason = StrikeReason.NotOnTarget;
        }

        _actionLog.Add(action);
        if (action.StrikeReason != StrikeReason.None) LogActions();
    }

    private void LogActions()
    {
        Debug.LogFormat("[3d Tunnels #{0}] You got a strike. Action log:", _moduleId);

        for (var i = 0; i < _actionLog.Count; i++)
        {
            var action = _actionLog[i];
            var msg = "";

            if (i == 0) {
                msg += "Starting at " + _symbolNames[action.StartLocation.ToInt()] + ". ";
                msg += GetOrientationDescription(action.StartLocation, action.StartOrientation);
                if (action.LocationIsIdentified)
                    msg += " This is the most recent location where the symbol is shown on the module. ";
            }

            msg += "Pressing " + action.Button.ToString() + ". ";
            if (action.StrikeReason == StrikeReason.NotOnTarget)
                msg += "You are not at " + _symbolNames[_targetNodes[_currentTarget]] + ", you are at " + _symbolNames[_location.ToInt()] + "!";
            else if (action.Button != Button.Target)
            {
                msg += "New orientation: " + GetOrientationDescription(action.StartLocation, action.EndOrientation);
                if (action.StrikeReason == StrikeReason.FlyIntoWall)
                    msg += "Moving forward. You fly into a wall! ";
                else
                    msg += "Moving forward to " + _symbolNames[action.EndLocation.ToInt()] + ". ";
            }
            Debug.LogFormat("[3d Tunnels #{0}] {1}", _moduleId, msg);
        }
        _actionLog.Clear();
    }

    private string GetOrientationDescription(Location location, ShipDirection direction)
    {
        var msg = "";
        var right = direction.GetLocationAt(new Axis(Dimension.X, 1), location);
        var left = direction.GetLocationAt(new Axis(Dimension.X, -1), location);
        var forwards = direction.GetLocationAt(new Axis(Dimension.Y, 1), location);
        var backwards = direction.GetLocationAt(new Axis(Dimension.Y, -1), location);
        var up = direction.GetLocationAt(new Axis(Dimension.Z, 1), location);
        var down = direction.GetLocationAt(new Axis(Dimension.Z, -1), location);
        var zag = direction.GetLocationAt(new Axis(Dimension.W, 1), location);
        var zig = direction.GetLocationAt(new Axis(Dimension.W, -1), location);
        return msg;
    }

    private IEnumerator RotateSymbol()
    {
        const float durationPerPing = 10f;

        Vector3 localEulerAngles = Symbol.transform.localEulerAngles;
        var time = 0f;

        while (true)
        {
            yield return null;

            time += Time.deltaTime;
            localEulerAngles.z = time / durationPerPing * -360;
            Symbol.transform.localEulerAngles = localEulerAngles;
        }
    }

    private IEnumerator ScaleSymbol()
    {
        const float durationPerPing = 2f;

        Vector3 localScale = Symbol.transform.localScale;
        float scaleDirection = 1f;

        while (true)
        {
            for (float time = 0f; time < durationPerPing; time += Time.deltaTime)
            {
                yield return null;

                localScale.x = Mathf.SmoothStep(-scaleDirection, scaleDirection, time / durationPerPing);
                Symbol.transform.localScale = localScale;
            }

            scaleDirection *= -1f;
        }
    }

    private string TwitchHelpMessage = @"Use '!{0} move u d l r i a' to move around the grid. Use '!{0} submit' to press the goal button.";

    IEnumerator ProcessTwitchCommand(string command)
    {
        var parts = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 1 && parts[0] == "move" && parts.Skip(1).All(part => part.Length == 1 && "udlria".Contains(part)))
        {
            yield return null;

            for (var i = 1; i < parts.Length; i++)
            {
                switch (parts[i])
                {
                    case "u":
                        PressButton(Button.Up);
                        break;
                    case "d":
                        PressButton(Button.Down);
                        break;
                    case "l":
                        PressButton(Button.Left);
                        break;
                    case "r":
                        PressButton(Button.Right);
                        break;
                    case "i":
                        PressButton(Button.Zig);
                        break;
                    case "a":
                        PressButton(Button.Zag);
                        break;
                }
                yield return new WaitForSeconds(.2f);
            }
        }
        else if (parts.Length == 1 && parts[0] == "submit")
        {
            yield return null;
            PressTargetButton();
        }
    }

    class Action
    {
        public Location StartLocation { get; set; }
        public ShipDirection StartOrientation { get; set; }
        public bool LocationIsIdentified { get; set; }
        public Button Button { get; set; }
        public Location EndLocation { get; set; }
        public ShipDirection EndOrientation { get; set; }
        public StrikeReason StrikeReason { get; set; }
    }
}
