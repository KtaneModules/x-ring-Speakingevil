using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class XRingScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMSelectable button;
    public Transform scanner;
    public GameObject[] symbols;
    public Transform spiralbar;
    public Renderer spiral;

    private int[] symbselect = new int[5] { 0, 0, 0, 0, -1};
    private List<int> order = new List<int> { 0, 1, 2, 3, 4 };
    private bool[] inout = new bool[5];
    private int disp;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        order = order.Shuffle();
        for (int i = 0; i < 5; i++)
            inout[i] = Random.Range(0, 2) == 0;
        spiral.enabled = false;
        for (int i = 0; i < 64; i++)
            symbols[i].SetActive(false);
        int[] centre = new int[2];
        for (int i = 0; i < 2; i++)
            centre[i] = Random.Range(2, 13);
        int[,] pos = new int[4, 2];
        int[] bounds = new int[2] { Mathf.Min(centre[0], 14 - centre[0]), Mathf.Min(centre[1], 14 - centre[1]) };
        int[][] offsets = new int[2][] { new int[2], new int[2] };
        while (offsets[0][0] % 2 != centre[0] % 2 || offsets[0][1] % 2 != centre[1] % 2 || (offsets[0][0] == 0 && offsets[0][1] == 0))
            offsets[0] = new int[2] { Random.Range(-bounds[0], bounds[0] + 1), Random.Range(-bounds[1], bounds[1] + 1) };
        while (offsets[1][0] % 2 != centre[0] % 2 || offsets[1][1] % 2 != centre[1] % 2 || ((offsets[0][0] == 0 ? 30 : (float)offsets[0][1] / offsets[0][0]) == (offsets[1][0] == 0 ? 30 : (float)offsets[1][1] / offsets[1][0])) || (offsets[1][0] == 0 && offsets[1][1] == 0))
            offsets[1] = new int[2] { Random.Range(-bounds[0], bounds[0] + 1), Random.Range(-bounds[1], bounds[1] + 1) };
        pos = new int[4, 2] { { centre[0] - offsets[0][0], centre[1] - offsets[0][1] }, { centre[0] + offsets[0][0], centre[1] + offsets[0][1] }, { centre[0] - offsets[1][0], centre[1] - offsets[1][1] }, { centre[0] + offsets[1][0], centre[1] + offsets[1][1] } };
        for (int i = 0; i < 4; i++)
            symbselect[i] = (pos[i, 0] * 4) + (pos[i, 1] / 2);
        symbselect[4] = Remain(centre, offsets);
        Debug.LogFormat("[X-Ring #{0}] The scanned symbols are: {1}", moduleID, string.Join(", ", order.Select(x => "ABCDEFGH"[symbselect[x] % 8] + ((symbselect[x] / 8) + 1).ToString()).ToArray()));
        Debug.LogFormat("[X-Ring #{0}] Press the button when the {1} display is shown.", moduleID, new string[] { "first", "second", "third", "fourth", "fifth"}[order.IndexOf(4)]);
        button.OnInteract = delegate () 
        {
            if (!moduleSolved)
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
                button.AddInteractionPunch(0.5f);
                Debug.LogFormat("[X-Ring #{0}] {1} display submitted.", moduleID, new string[] { "First", "Second", "Third", "Fourth", "Fifth", "No" }[disp]);
                if (order[disp] == 4)
                {
                    moduleSolved = true;
                    Audio.PlaySoundAtTransform("Solve", transform);
                    for (int i = 0; i < 64; i++)
                        symbols[i].SetActive(false);
                    spiral.enabled = true;
                    module.HandlePass();
                }
                else
                    module.HandleStrike();
            }
            return false;
        };
        StartCoroutine(Seq());
        StartCoroutine(Spiral());
    }

    private int Remain(int[] centre, int[][] offsets)
    {
        List<int> w = Enumerable.Range(0, 64).Except(symbselect).ToList();
        Debug.Log("Verts: " + Enumerable.Range(0, 4).Select(x => "(" + ((symbselect[x] % 8) + 1).ToString() + "," + ((symbselect[x] / 8) + 1).ToString() + ")").Join());
        int[,] deny = new int[8, 2]
        {
            { centre[0] - ((2 * offsets[0][0]) + offsets[1][0]), centre[1] - ((2 * offsets[0][1]) + offsets[1][1]) },
            { centre[0] - ((2 * offsets[1][0]) + offsets[0][0]), centre[1] - ((2 * offsets[1][1]) + offsets[0][1]) },
            { centre[0] - ((2 * offsets[0][0]) - offsets[1][0]), centre[1] - ((2 * offsets[0][1]) - offsets[1][1]) },
            { centre[0] - ((2 * offsets[1][0]) - offsets[0][0]), centre[1] - ((2 * offsets[1][1]) - offsets[0][1]) },
            { centre[0] + ((2 * offsets[0][0]) + offsets[1][0]), centre[1] + ((2 * offsets[0][1]) + offsets[1][1]) },
            { centre[0] + ((2 * offsets[1][0]) + offsets[0][0]), centre[1] + ((2 * offsets[1][1]) + offsets[0][1]) },
            { centre[0] + ((2 * offsets[0][0]) - offsets[1][0]), centre[1] + ((2 * offsets[0][1]) - offsets[1][1]) },
            { centre[0] + ((2 * offsets[1][0]) - offsets[0][0]), centre[1] + ((2 * offsets[1][1]) - offsets[0][1]) }
        };
        for(int i = 0; i < 8; i++)
        {
            deny[i, 0] /= 2;
            deny[i, 1] /= 2;
            if (deny[i, 0] < 0 || deny[i, 0] > 7 || deny[i, 1] < 0 || deny[i, 1] > 7)
                continue;
            int d = (deny[i, 0] * 8) + deny[i, 1];
            if (w.Contains(d))
                w.Remove(d);
        }
        Debug.Log("Remove " + Enumerable.Range(0, 8).Select(x => "(" + (deny[x, 1] + 1).ToString() + "," + (deny[x, 0] + 1).ToString() + ")").Join());
        return w.PickRandom();
    }

    private IEnumerator Seq()
    {
        while (!moduleSolved)
        {
            if (disp == 5)
                yield return new WaitForSeconds(1);
            else
            {
                symbols[symbselect[order[disp]]].SetActive(true);
                float elapsed = 0f;
                while(elapsed < 1f)
                {
                    elapsed += Time.deltaTime / 2.5f;
                    float len = 1 / (inout[disp] ? elapsed : 1 - elapsed);
                    scanner.localScale = new Vector3(len, 1, len);
                    yield return null;
                }
                symbols[symbselect[order[disp]]].SetActive(false);
                yield return new WaitForSeconds(0.25f);
            }
            disp = (disp + 1) % 6;
        }
    }

    private IEnumerator Spiral()
    {
        while (true)
        {
            spiralbar.Rotate(Vector3.up, Time.deltaTime * 180);
            yield return null;
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <A-H><1-5> [Presses button when the specified symbol is displayed.]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToUpperInvariant();
        if(command.Length != 2 || !"ABCDEFGH".Contains(command[0]) || !"12345678".Contains(command[1]))
        {
            yield return "sendtochaterror!f \"" + command + "\" is not a valid coordinate.";
            yield break;
        }
        int coord = (command[1] - '1') * 8 + (command[0] - 'A');
        if (!symbselect.Contains(coord))
        {
            yield return "unsubmittablepenalty";
            yield return "sendtochat!f " + command + "is not displayed on the module.";
            yield break;
        }
        yield return null;
        while (disp > 4 || symbselect[order[disp]] != coord)
            yield return "trycancel";
        button.OnInteract();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (disp > 4 || order[disp] != 4)
            yield return true;
        button.OnInteract();
    }
}
