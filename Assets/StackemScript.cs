using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class StackemScript : MonoBehaviour
{

    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    public KMSelectable[] Selectors;
    public KMSelectable[] Input;
    public KMSelectable Submit;

    public TextMesh[] eNumberTxT;
    public TextMesh[] iNumberTxT;

    public Material[] iNumberMat;

    public GameObject[] CubePrefabs;

    private float[] selRot = new[] { 0f, 0f, 0f, 0f, 0f, 0f };

    private bool[] selRotActive = new[] { false, false, false, false, false, false };
    private bool deleteActive = false;

    private int[] CubesSpawned = new[] { -1, -1, -1, -1 };
    private double[] CubeValues = new[] { 0d, 0d, 0d, 0d, 0d, 0d };
    private int[] ExpNum = new[] { 0, 0, 0, 0 };
    private int curPos;
    private bool[] loop = new[] { true, true, true, true };
    private bool[] solve = new[] { false, false, false, false };

    private int curSel = -1;

    private bool SubAc = false;

    private List<GameObject>[] SpawnedCubes = new List<GameObject>[]
    {
        new List<GameObject>(),
        new List<GameObject>(),
        new List<GameObject>(),
        new List<GameObject>()
    };

    private List<int>[] InNum = new List<int>[]
    {
        new List<int>(),
        new List<int>(),
        new List<int>(),
        new List<int>()
    };
    private bool solveAnimationDone = false;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < selRot.Length; i++)
        {
            selRot[i] = Random.Range(.5f, 1f) * (Random.Range(0, 2) * 2 - 1);
        }

        for (int i = 0; i < Input.Length; i++)
        {
            Input[i].OnInteract += InputClicked(i);
        }

        for (int i = 0; i < Selectors.Length; i++)
        {
            Selectors[i].OnInteract += SelectorsClicked(i);
        }

        Submit.OnInteract = SubmitClicked();

    }

    private KMSelectable.OnInteractHandler SubmitClicked()
    {
        return delegate ()
        {
            if (SubAc)
                return false;

            SubAc = true;
            selRotActive[curPos] = false;

            StartCoroutine(SubmitActive());
            StartCoroutine(SubmitCalc());

            return false;
        };
    }

    private KMSelectable.OnInteractHandler InputClicked(int pos)
    {
        return delegate ()
        {
            if (SubAc)
                return false;

            if (curSel == -1)
                return false;

            if (deleteActive)
            {

                if (CubesSpawned[pos] == -1)
                    return false;

                Input[pos].GetComponent<Transform>().localPosition = SpawnedCubes[pos][CubesSpawned[pos]].GetComponent<Transform>().localPosition;

                Destroy(SpawnedCubes[pos][CubesSpawned[pos]]);

                SpawnedCubes[pos].Remove(SpawnedCubes[pos][CubesSpawned[pos]]);

                InNum[pos].RemoveAt(CubesSpawned[pos]);

                CubesSpawned[pos] -= 1;

                return false;
            }

            if (CubesSpawned[pos] == 4)
                return false;

            SpawnedCubes[pos].Add(Instantiate(CubePrefabs[curSel]));
            CubesSpawned[pos] += 1;

            Vector3 inputLP = Input[pos].GetComponent<Transform>().localPosition;
            Vector3 CubeSpawnedLS = SpawnedCubes[pos][CubesSpawned[pos]].GetComponent<Transform>().localScale;

            SpawnedCubes[pos][CubesSpawned[pos]].transform.parent = Input[pos].transform.parent;
            SpawnedCubes[pos][CubesSpawned[pos]].GetComponent<Transform>().localPosition = inputLP;
            SpawnedCubes[pos][CubesSpawned[pos]].GetComponent<Transform>().localRotation = Quaternion.identity;
            SpawnedCubes[pos][CubesSpawned[pos]].GetComponent<Transform>().localScale = Input[pos].GetComponent<Transform>().localScale;

            inputLP.y += CubeSpawnedLS.y + 0.0045f;
            Input[pos].GetComponent<Transform>().localPosition = inputLP;

            InNum[pos].Add((int)CubeValues[curPos]);

            return false;
        };
    }

    private KMSelectable.OnInteractHandler SelectorsClicked(int pos)
    {
        return delegate ()
        {
            if (SubAc)
                return false;

            if (pos > 5)
            {
                deleteActive = true;
                selRotActive[curPos] = false;

                return false;
            }

            deleteActive = false;
            selRotActive[curPos] = false;
            selRotActive[pos] = true;

            curPos = pos;
            curSel = pos;

            return false;
        };
    }

    void Start()
    {

        string ser = SertoInt(BombInfo.GetSerialNumber());
        Debug.LogFormat(@"[Stack'em #{0}] Converted SN: {1}", moduleId, ser);
        char[] arr = ser.ToCharArray();
        Array.Reverse(arr);
        string ser2 = new string(arr);
        Debug.LogFormat(@"[Stack'em #{0}] Reversed converted SN: {1}", moduleId, ser2);
        var result = ser2
        .Select((ch, ix) => new { Character = ch, Index = ix })
        .OrderBy(inf => inf.Character)
        .Select((inf, ix) => new { inf.Index, Character = (char)(ix + '1') })
        .OrderBy(inf => inf.Index)
        .Select(inf => inf.Character)
        .Join("");
        Debug.LogFormat(@"[Stack'em #{0}] Final sequence of cube values: {1}", moduleId, result);

        for (int i = 0; i < CubeValues.Length; i++)
        {
            CubeValues[i] = char.GetNumericValue(result[i]);
        }

        GetExp();
    }

    void Update()
    {

        for (int i = 0; i < selRot.Length; i++)
        {
            if (!selRotActive[i])
                continue;
            Vector3 SelRot = Selectors[i].GetComponent<Transform>().localEulerAngles;
            SelRot.y += selRot[i];
            Selectors[i].GetComponent<Transform>().localEulerAngles = SelRot;
        }
    }

    private string SertoInt(string s)
    {
        string newS = s.Replace("A", "1").Replace("B", "2").Replace("C", "3").Replace("D", "4").Replace("E", "5").Replace("F", "6").Replace("G", "1").Replace("H", "2")
        .Replace("I", "3").Replace("J", "4").Replace("K", "5").Replace("L", "6").Replace("M", "1").Replace("N", "2").Replace("O", "3").Replace("P", "4")
        .Replace("Q", "5").Replace("R", "6").Replace("S", "1").Replace("T", "2").Replace("U", "3").Replace("V", "4").Replace("W", "5")
        .Replace("X", "6").Replace("Y", "1").Replace("Z", "2").Replace("0", "6").Replace("7", "1").Replace("8", "2").Replace("9", "3");

        return newS;
    }

    private IEnumerator SubmitActive()
    {
        while (loop[0])
        {
            iNumberTxT[0].text = Random.Range(0, 100).ToString();
            iNumberTxT[1].text = Random.Range(0, 100).ToString();
            iNumberTxT[2].text = Random.Range(0, 100).ToString();
            iNumberTxT[3].text = Random.Range(0, 100).ToString();
            yield return new WaitForSeconds(0.05f);
        }
        while (loop[1])
        {
            iNumberTxT[1].text = Random.Range(0, 100).ToString();
            iNumberTxT[2].text = Random.Range(0, 100).ToString();
            iNumberTxT[3].text = Random.Range(0, 100).ToString();
            yield return new WaitForSeconds(0.05f);
        }
        while (loop[2])
        {
            iNumberTxT[2].text = Random.Range(0, 100).ToString();
            iNumberTxT[3].text = Random.Range(0, 100).ToString();
            yield return new WaitForSeconds(0.05f);
        }
        while (loop[3])
        {
            iNumberTxT[3].text = Random.Range(0, 100).ToString();
            yield return new WaitForSeconds(0.05f);
        }
        yield break;
    }

    private IEnumerator SubmitCalc()
    {
        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForSeconds(2f);
            loop[i] = false;
            iNumberTxT[i].text = InNum[i].Sum().ToString();
            if (ExpNum[i] == InNum[i].Sum())
            {
                iNumberTxT[i].color = new Color32(0, 255, 0, 255);
                solve[i] = true;
            }
            else
            {
                iNumberTxT[i].color = new Color32(255, 0, 0, 255);
                solve[i] = false;
            }
        }
        Debug.LogFormat(@"[Stack'em #{0}] Your entered sums are: {1}", moduleId, InNum.Select(x => x.Sum()).Join(", "));
        for (int i = 0; i < 4; i++)
        {
            if (solve[i])
                continue;
            yield return new WaitForSeconds(1f);
            BombModule.HandleStrike();
            ResetModule();
            yield break;

        }
        solveAnimationDone = true;
        Debug.LogFormat(@"[Stack'em #{0}] Solve!", moduleId);
        BombModule.HandlePass();
    }

    private void ResetModule()
    {

        for (int i = 0; i < 4; i++)
        {
            iNumberTxT[i].color = new Color32(0, 255, 0, 255);
            iNumberTxT[i].text = "";
            loop[i] = true;
            solve[i] = false;
            ExpNum[i] = 0;
            while (CubesSpawned[i] != -1)
            {
                Input[i].GetComponent<Transform>().localPosition = SpawnedCubes[i][CubesSpawned[i]].GetComponent<Transform>().localPosition;

                Destroy(SpawnedCubes[i][CubesSpawned[i]]);

                SpawnedCubes[i].Remove(SpawnedCubes[i][CubesSpawned[i]]);

                InNum[i].RemoveAt(CubesSpawned[i]);

                CubesSpawned[i] -= 1;
            }
        }

        curPos = 0;
        curSel = -1;

        Debug.LogFormat(@"[Stack'em #{0}] You striked, resetting module.", moduleId);
        GetExp();

        SubAc = false;
    }

    private void GetExp()
    {
        for (int i = 0; i < eNumberTxT.Length; i++)
        {
            ExpNum[i] = Random.Range(1, 31);
            eNumberTxT[i].text = ExpNum[i].ToString();
        }
        Debug.LogFormat(@"[Stack'em #{0}] Your target sums are: {1}", moduleId, ExpNum.Join(", "));
        return;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} red 2 4, b 1 4 [put 4 red cubes in slot 2 and 4 blue cubes in slot 1] | !{0} delete 3 2, del 2 5 [delete 2 cubes from slot 3 and 5 cubes from slot 2] | !{0} submit";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*((?<color>b|g|o|m|r|y|blue|green|orange|magenta|red|yellow|del|delete|erase)\s+(?<slot>[1-4])\s+(?<num>[1-5])\,*\s*)*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var selectables = new List<KMSelectable>();
            for (int i = 0; i < m.Groups["color"].Captures.Count; i++)
            {
                var color = m.Groups["color"].Captures[i].Value;
                if (color.ToLowerInvariant() == "del" || color.ToLowerInvariant() == "delete" || color.ToLowerInvariant() == "erase")
                    selectables.Add(Selectors[6]);
                else
                    selectables.Add(Selectors["bgomry".IndexOf(char.ToLowerInvariant(color[0]))]);
                var slot = int.Parse(m.Groups["slot"].Captures[i].Value) - 1;
                var num = int.Parse(m.Groups["num"].Captures[i].Value);

                for (int j = 0; j < num; j++)
                    selectables.Add(Input[slot]);
            }
            yield return null;
            yield return selectables;
        }

        else if (Regex.IsMatch(command, @"^\s*(submit|enter|go|finish|ready)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            yield return Enumerable.Range(0, 4).All(i => ExpNum[i] == InNum[i].Sum()) ? "solve" : "strike";
            yield return new[] { Submit };
        }
        else
        {
            yield return "sendtochaterror Invalid Command";
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {

        Debug.LogFormat(@"[Stack'em #{0}] The module was force solved by TP.", moduleId);

        
        for (int i = 0; i < 4; i++)
        {
            while(InNum[i].Sum() > ExpNum[i])
            {
                Selectors[6].OnInteract();
                yield return new WaitForSeconds(.1f);
                Input[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (ExpNum[i] - InNum[i].Sum() >= 6)
            {
                Selectors[Array.IndexOf(CubeValues, 6)].OnInteract();
                yield return new WaitForSeconds(.1f);
                Input[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (ExpNum[i] - InNum[i].Sum() >= 5)
            {
                Selectors[Array.IndexOf(CubeValues, 5)].OnInteract();
                yield return new WaitForSeconds(.1f);
                Input[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (ExpNum[i] - InNum[i].Sum() >= 4)
            {
                Selectors[Array.IndexOf(CubeValues, 4)].OnInteract();
                yield return new WaitForSeconds(.1f);
                Input[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (ExpNum[i] - InNum[i].Sum() >= 3)
            {
                Selectors[Array.IndexOf(CubeValues, 3)].OnInteract();
                yield return new WaitForSeconds(.1f);
                Input[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (ExpNum[i] - InNum[i].Sum() >= 2)
            {
                Selectors[Array.IndexOf(CubeValues, 2)].OnInteract();
                yield return new WaitForSeconds(.1f);
                Input[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (ExpNum[i] - InNum[i].Sum() >= 1)
            {
                Selectors[Array.IndexOf(CubeValues, 1)].OnInteract();
                yield return new WaitForSeconds(.1f);
                Input[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        Submit.OnInteract();
        while (!solveAnimationDone)
            yield return true;
    }
}
