/*
*プログラム: 
*   最終更新日:
*       11.11.2016
*
*   説明:
*       テキスト管理をします
*       これを使うと多言語表示に対応できます
*
*   パラメータ説明:
*       dontDestoroyObjectOnLoad:
*           true: シーン間でデストロイされませんA
*           false: シーン間でデストロイされます
*
*       languageID:
*           表示言語を変えるときに用います
*
*       coreLanguagePackFileList:
*           ここで登録された言語パックファイルはシーン間でデストロイされません
*
*           packFileName:
*               packFileが存在するパス
*               'Assets/'から始まるパス
*
*       sceneList:
*           各シーンごとで使用する言語パックをここで登録します
*           シーン間でデストロイされます
*
*           sceneName:
*               シーンの名前
*
*           languagePackFileList:
*               packFileName:
*                   packFileが存在するパス
*                   'Assets/'から始まるパス
*
*   LanguagePackFileの作り方:
*       Textファイルを用意します
*       Label名を書きます
*           例:
                <Label>Mes0
*
*       Listを書きます
*           例:
                <Label>Mes0
                <List>Test
                <List>テスト

*       上二つをLabel数繰り返します
*           例:
                <Label>Mes0
                <List>Test
                <List>テスト

                <Label>Mes1
                <List>Hello
                <List>やあ

*       UTF8形式で保存します
*           
*      補足:
*           上の場合languageIDが0のとき英語, LanguageIDが1のとき日本語です
*
*   使用例:
*       上のPackFileをロードしているとします
*       LanguageID = 0とします
*       あるスクリプト内:
            Debug.Log(LanguagePackManager.instance.GetString("Mes0"));
*       結果:
*           Test
*
*   更新履歴:
*       4.5.2016:
*           プログラムの完成
*
*       4.11.2016; 4.12.2016; 5.5.2016
*           スクリプト修正
*
*       5.17.2016:
*           仕様変更
*               言語パックのロードのタイミングを変更
*
*       7.9.2016:
*           同じシーンを再読み込みしたときそのシーンで登録される言語パックが破棄される問題を修正
*           
*       11.11.2016:
*           UnityUpdateに伴う修正; OnLevelWasLoadedの代わりにSceneManagerを使用
*/



/*
 *連絡
 *  2018-2-15(Ken):
 *   IDを非静的
 *   理由:
 *    inspectorで変更できない.
 * 
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;

public class LanguagePackManager : MonoBehaviour
{
    public static LanguagePackManager Instance { get; private set; }

    //===外部パラメータ(inspector表示)===============================================
    public int languageID = 0;
    public List<LanguagePackFile> coreLanguagePackFileList;
    public List<SceneSetting> sceneList;

    //===外部パラメータ==============================================================
    [System.Serializable]
    public class SceneSetting
    {
        public string sceneName;
        public List<LanguagePackFile> languagePackFileList;

    }

    [System.Serializable]
    public class LanguagePackFile
    {
        public string packFileName = "";
        public LanguagePackFile(string fileName)
        {
            packFileName = fileName;
        }
    }

    //===内部パラメータ==============================================================
    string[] labelSeparators = new string[] { "<Label>" };
    string[] listSeparators = new string[] { "<List>" };

    class LanguagePack
    {
        public bool dontDestroyOnLoad = false;
        public Dictionary<string, List<string>> labelList = new Dictionary<string, List<string>>();
    }
    List<LanguagePack> languagePackList = new List<LanguagePack>();

    //===コード======================================================================
    void Awake()
    {
        foreach (LanguagePackFile file in coreLanguagePackFileList)
        {
            LoadFile(file, true);
        }

        SceneManager.sceneLoaded += OnSceneWasLoaded;
        DontDestroyOnLoad(this);
        Instance = this;
    }

    void Update()
    {
        /*
        Debug.Log("files: " + languagePackFileList.Count);
        Debug.Log("packs: " + languagePackList.Count);
        
        foreach(LanguagePack pack in languagePackList)
        {
            Debug.Log("labels: " + pack.labelList.Count);
            foreach(KeyValuePair<string, List<string>> label in pack.labelList)
            {
                Debug.Log("label: " + label.Key);
            }
        }
        */
    }

    //シーンがロードされたとき
    void OnSceneWasLoaded(Scene scenename, LoadSceneMode SceneMode)
    {
        SetManager();
    }

    //シーンに応じてManagerの情報を更新します
    void SetManager()
    {
        UnLoadFiles();

        string sceneName = SceneManager.GetActiveScene().name;

        foreach (SceneSetting scene in sceneList)
        {
            if (scene.sceneName == sceneName)
            {
                foreach (LanguagePackFile file in scene.languagePackFileList)
                {
                    LoadFile(file, false);
                }
            }
        }
    }

    //PackFileをロードします
    public bool LoadFile(LanguagePackFile file, bool dontDestroy)
    {
        try
        {
            //filePathを取得
            string filePath = Application.dataPath + "/" + file.packFileName;
            //Debug.Log(filePath);
            //FileInfo fileInfo = new FileInfo(filePath);
            //StreamReader streamReader = new StreamReader(fileInfo.Open(FileMode.Open, FileAccess.Read), Encoding.UTF8);
            using (StreamReader reader = new StreamReader(@filePath, Encoding.UTF8))
            {
                //改行を消去
                string text = reader.ReadToEnd().Replace("\r", "").Replace("\n", "");

                //LanguagePackListに追加
                AddLanguagePack(text, file, dontDestroy);

                reader.Close();
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LanguagePackManager.LoadFiles] Failed > " + e.Message + "");

            return false;
        }
    }

    private void UnLoadFiles()
    {
        //languagePackListを整理
        List<LanguagePack> languagePackListRearrange = new List<LanguagePack>();
        foreach (LanguagePack pack in languagePackList)
        {
            if (pack.dontDestroyOnLoad)
            {
                languagePackListRearrange.Add(pack);
            }
        }
        languagePackList = languagePackListRearrange;
    }

    private void AddLabel(Dictionary<string, List<string>> labelList, string key, List<string> textList)
    {
        if (labelList.ContainsKey(key))
        {
            labelList[key] = textList;
        }
        else
        {
            labelList.Add(key, textList);
        }
    }

    //languagePackをlanguagePackListに追加
    private void AddLanguagePack(string text, LanguagePackFile packFile, bool dontDestroy)
    {
        LanguagePack pack = new LanguagePack();
        pack.dontDestroyOnLoad = dontDestroy;

        string[] labelBlockList = text.Split(labelSeparators, System.StringSplitOptions.None);
        for (int i = 1; i < labelBlockList.Length; i++)
        {
            string[] listBlockList = labelBlockList[i].Split(listSeparators, System.StringSplitOptions.None);
            if (listBlockList.Length >= 1)
            {
                string key = "";
                key = listBlockList[0];
                List<string> textList = new List<string>();
                for (int j = 1; j < listBlockList.Length; j++)
                {
                    textList.Add(listBlockList[j]);
                }
                AddLabel(pack.labelList, key, textList);
            }
        }

        languagePackList.Add(pack);
    }

    //
    //  説明:
    //      指定したlabel内のテキストを返します
    //      
    public string GetString(string label)
    {
        foreach (LanguagePack pack in languagePackList)
        {
            if (pack.labelList.ContainsKey(label))
            {
                if (languageID < pack.labelList[label].Count)
                {
                    return pack.labelList[label][languageID].Replace("<n>", System.Environment.NewLine);
                }
            }
        }

        return "";
    }
}
