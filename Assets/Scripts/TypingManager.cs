using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

// 画面にあるテキストの文字を変更したい
public class TypingManager : MonoBehaviour
{
    // 画面にあるテキストを持ってくる
    [SerializeField] Text furiganaText; // ふりがな用のテキスト
    [SerializeField] Text questionText; // 問題用のテキスト
    [SerializeField] Text answerText; // 答え用のテキスト

    // テキストデータを読み込む
    [SerializeField] TextAsset _furiganaFile;
    [SerializeField] TextAsset _questionFile;
    //[SerializeField] TextAsset _answer;

    // テキストデータを格納するためのリスト
    private List<string> _furiganaList = new List<string>();
    private List<string> _questionList = new List<string>();
    //private List<string> _aList = new List<string>();

    // 何番目か指定するためのstring
    private string _furiganaString;
    private string _questionString;
    private string _answerString;

    // 何番目の問題か
    private int _questionNumber;

    // 問題の何文字目か
    private int _answerNumber;

    // 合ってるかどうかの判断
    bool isCorrect;

    // Shiftが押されてるかどうかの判定
    bool isShift;

    private ChangeDictionary cd;

    // しんぶん→"si","n","bu","n"
    // しんぶん→"shi","n","bu","n"
    // {0,0,1,2,2,3}
    // {0,1,0,0,1,0}
    private List<string> _romSliceList = new List<string>();
    private List<int> _furiganaCountList = new List<int>();
    private List<int> _romNumList = new List<int>();

    // ゲームを始めた時に1度だけ呼ばれるもの
    void Start()
    {
        cd = GetComponent<ChangeDictionary>();

        // テキストデータをリストに入れる
        SetList();

        // 問題を出す
        OutPut();
    }

    // Update is called once per frame
    void Update()
    {
        // 入力された時に判断する
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                return;
            }

            isCorrect = false;
            int furiganaCount = _furiganaCountList[_answerNumber];

            // 入力されたキーが何か確認する
            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(code))
                {
                    Debug.Log(code);
                    break;
                }
            }

            // 完全に合ってたら正解！
            // し s  i
            if (Input.GetKeyDown(_answerString[_answerNumber].ToString()) || GetKeyReturn() == _answerString[_answerNumber].ToString())
            {
                // trueにする
                isCorrect = true;

                // 正解
                Correct();

                // 最後の文字に正解したら
                if (_answerNumber >= _answerString.Length)
                {
                    // 問題を変える
                    OutPut();
                }
            }
            else if (Input.GetKeyDown("n") && furiganaCount > 0 && _romSliceList[furiganaCount - 1] == "n")
            {
                // nnにしたい
                _romSliceList[furiganaCount - 1] = "nn";
                //_answerString = string.Join("", _romSliceList);
                _answerString = string.Join("", GetRomSliceListWithoutSkip());

                RecreateList(_romSliceList);

                // trueにする
                isCorrect = true;

                // 正解
                Correct();

                // 最後の文字に正解したら
                if (_answerNumber >= _answerString.Length)
                {
                    // 問題を変える
                    OutPut();
                }
            }
            else
            {
                // し →si, ci, shi
                // 柔軟な入力があるかどうか
                // 「し」→ "si" , "shi"
                // 今どの ふりがな を打たないといけないのかを取得する
                string currentFurigana = _furiganaString[furiganaCount].ToString();

                if (furiganaCount < _furiganaString.Length - 1)
                {
                    // 2文字を考慮した候補検索「しゃ」
                    string addNextMoji = _furiganaString[furiganaCount].ToString() + _furiganaString[furiganaCount + 1].ToString();
                    CheckIrregularType(addNextMoji, furiganaCount, false);
                }

                if (!isCorrect)
                {
                    // 今まで通りの候補検索「し」「ゃ」
                    string moji = _furiganaString[furiganaCount].ToString();
                    CheckIrregularType(moji, furiganaCount, true);
                }
            }

            // 正解じゃなかったら
            if (!isCorrect)
            {
                // 失敗
                Miss();
            }
        }
    }

    // 入力されたキーを正しいものに変換させる
    string GetKeyReturn()
    {
        isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isShift)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                return "!";
            }
        }
        return "";
    }

    void CheckIrregularType(string currentFurigana, int furiganaCount, bool addSmallMoji)
    {
        if (cd.dic.ContainsKey(currentFurigana))
        {

            List<string> stringList = cd.dic[currentFurigana]; // ci, shi
            Debug.Log(string.Join(",", stringList));

            // stringList[0] ci, stringList[1] shi
            for (int i = 0; i < stringList.Count; i++)
            {
                string rom = stringList[i];
                int romNum = _romNumList[_answerNumber];

                bool preCheck = true;

                for (int j = 0; j < romNum; j++)
                {
                    if (rom[j] != _romSliceList[furiganaCount][j])
                    {
                        preCheck = false;
                    }
                }

                if (preCheck && Input.GetKeyDown(rom[romNum].ToString()))
                {
                    _romSliceList[furiganaCount] = rom;
                    _answerString = string.Join("", GetRomSliceListWithoutSkip());

                    RecreateList(_romSliceList);

                    // trueにする
                    isCorrect = true;

                    if (addSmallMoji)
                    {
                        AddSmallMoji();
                    }

                    // 正解
                    Correct();

                    // 最後の文字に正解したら
                    if (_answerNumber >= _answerString.Length)
                    {
                        // 問題を変える
                        OutPut();
                    }
                    break;
                }
            }
        }

    }

    void SetList()
    {
        string[] _furiganaArray = _furiganaFile.text.Split('\n');
        _furiganaList.AddRange(_furiganaArray);

        string[] _questionArray = _questionFile.text.Split('\n');
        _questionList.AddRange(_questionArray);

        //string[] _aArray = _answer.text.Split('\n');
        //_aList.AddRange(_aArray);
    }

    // 柔軟な入力をしたときに次の文字が小文字なら小文字を挿入する
    void AddSmallMoji()
    {
        int nextMojiNum = _furiganaCountList[_answerNumber] + 1;

        // もし次の文字がなければ処理をしない
        if (_furiganaString.Length - 1 < nextMojiNum)
        {
            return;
        }

        string nextMoji = _furiganaString[nextMojiNum].ToString();
        string a = cd.dic[nextMoji][0];

        // もしaの0番目がxでもlでもなければ処理をしない
        if (a[0] != 'x' && a[0] != 'l')
        {
            return;
        }

        // romSliceListに挿入と表示の反映
        _romSliceList.Insert(nextMojiNum, a);
        // SKIPを削除する
        _romSliceList.RemoveAt(nextMojiNum + 1);

        // 変更したリストを再度表示させる
        RecreateList(_romSliceList);
        _answerString = string.Join("", GetRomSliceListWithoutSkip());
    }

    // しんぶん→"shi","n","bu","n"
    // { 0, 0, 1, 2, 2, 3 }
    // { 0, 1, 0, 0, 1, 0 }
    void CreatRomSliceList(string moji)
    {
        _romSliceList.Clear();
        _furiganaCountList.Clear();
        _romNumList.Clear();

        // 「し」→「si」,「ん」→「n」
        for (int i = 0; i < moji.Length; i++)
        {
            string a = cd.dic[moji[i].ToString()][0];

            if (moji[i].ToString() == "ゃ" || moji[i].ToString() == "ゅ" || moji[i].ToString() == "ょ")
            {
                a = "SKIP";
            }
            else if (moji[i].ToString() == "っ" && i + 1 < moji.Length)
            {
                a = cd.dic[moji[i + 1].ToString()][0][0].ToString();
            }
            else if (i + 1 < moji.Length)
            {
                // 次の文字も含めて辞書から探す
                string addNextMoji = moji[i].ToString() + moji[i + 1].ToString();
                if (cd.dic.ContainsKey(addNextMoji))
                {
                    a = cd.dic[addNextMoji][0];
                }
            }

            _romSliceList.Add(a);

            if (a == "SKIP")
            {
                continue;
            }
            for (int j = 0; j < a.Length; j++)
            {
                _furiganaCountList.Add(i);
                _romNumList.Add(j);
            }
        }
        Debug.Log(string.Join(",", _romSliceList));
    }

    void RecreateList(List<string> romList)
    {
        _furiganaCountList.Clear();
        _romNumList.Clear();

        // 「し」→「si」,「ん」→「n」
        for (int i = 0; i < romList.Count; i++)
        {
            string a = romList[i];
            if (a == "SKIP")
            {
                continue;
            }
            for (int j = 0; j < a.Length; j++)
            {
                _furiganaCountList.Add(i);
                _romNumList.Add(j);
            }
        }
        //Debug.Log(string.Join(",", _romSliceList));
    }

    // SKIPなしの表示をさせるためのListを作り直す
    List<string> GetRomSliceListWithoutSkip()
    {
        List<string> returnList = new List<string>();
        foreach(string rom in _romSliceList)
        {
            if (rom == "SKIP")
            {
                continue;
            }
            returnList.Add(rom);
        }
        return returnList;
    }

    // 問題を出すための関数
    void OutPut()
    {
        // 0番目の文字に戻す
        _answerNumber = 0;

        // _qNumに０〜問題数の数までのランダムな数字を1つ入れる
        _questionNumber = UnityEngine.Random.Range(0, _questionList.Count);

        _furiganaString = _furiganaList[_questionNumber];
        _questionString = _questionList[_questionNumber];

        CreatRomSliceList(_furiganaString);

        _answerString = string.Join("", GetRomSliceListWithoutSkip());

        // 文字を変更する
        furiganaText.text = _furiganaString;
        questionText.text = _questionString;
        answerText.text = _answerString;

        //Debug.Log(string.Join("", _romSliceList));
    }

    // 正解用の関数
    void Correct()
    {
        // 正解した時の処理（やりたいこと）
        _answerNumber++;
        answerText.text = "<color=#6A6A6A>" + _answerString.Substring(0, _answerNumber) + "</color>" + _answerString.Substring(_answerNumber);
        //Debug.Log(_answerNumber);
    }

    // 間違え用の関数
    void Miss()
    {
        // 間違えた時にやりたいこと
        answerText.text = "<color=#6A6A6A>" + _answerString.Substring(0, _answerNumber) + "</color>"
            + "<color=#FF0000>" + _answerString.Substring(_answerNumber, 1) + "</color>"
            + _answerString.Substring(_answerNumber + 1);
    }
}
