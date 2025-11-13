using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;
using System;

namespace  UCF.Media.Editor.Tools
{
    public class ASTCEncoderWindow : EditorWindow
    {
        [MenuItem("il/Media/ASTC Encoder", false, 1)]
        public static void OpenASTCEncoderWindow()
        {
            GetWindow<ASTCEncoderWindow>().Show();
        }

        private enum Profile
        {
            [InspectorName("cl")] cl,
            [InspectorName("cs")] cs,
            [InspectorName("ch")] ch,
            [InspectorName("cH")] cH
        }

        private enum BlockSize
        {
            _4x4,
            _5x5,
            _6x6,
            _8x8,
            _10x10,
            _12x12
        }

        private enum Quality
        {
            fastest,
            fast,
            medium,
            thorough,
            verythorough,
            exhaustive,
        }

        private Profile selectedProfile;
        private BlockSize selectedBlockSize;
        private Quality selectedQuality;
        private bool isFlip;

        private Vector2 scrolPosition;

        private List<UnityEngine.Object> inputFileList;

        private void OnEnable()
        {
            this.titleContent = new GUIContent("ASTC Encoder Window");
            this.minSize = new Vector2(400f, 400f);

            inputFileList = new List<UnityEngine.Object>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Input file list", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("프로젝트 내 Texture 파일들을 Drag & Drop 하여 등록할 수 있습니다.", MessageType.Info);
            DragDrop();
            scrolPosition = EditorGUILayout.BeginScrollView(scrolPosition, EditorStyles.helpBox, GUILayout.Height(100));

            foreach (var file in inputFileList)
            {
                EditorGUILayout.ObjectField(file, typeof(Texture), false);
            }

            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Clear"))
            {
                inputFileList.Clear();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ASTC Encoder Options", EditorStyles.boldLabel);
            selectedProfile = (Profile)EditorGUILayout.EnumPopup("Profile", selectedProfile);
            selectedBlockSize = (BlockSize)EditorGUILayout.EnumPopup("BlockSize", selectedBlockSize);
            selectedQuality = (Quality)EditorGUILayout.EnumPopup("Quality", selectedQuality);
            isFlip = EditorGUILayout.Toggle("Flip", isFlip);
            if (selectedQuality == Quality.exhaustive && isFlip == false)
            {
                EditorGUILayout.HelpBox("Exhaustive 설정 사용 시 Flip 옵션 활성화가 필요합니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Encode"))
            {
                foreach (var file in inputFileList)
                {
                    RunCommand(file);
                }
                AssetDatabase.Refresh();
            }
        }

        private void DragDrop()
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                        {
                            if (AssetDatabase.Contains(obj))
                            {
                                if (obj is Texture)
                                {
                                    inputFileList.Add(obj);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private string CreateArgument(UnityEngine.Object inputObj)
        {
            DirectoryInfo outputDir = new DirectoryInfo(Path.Combine(Application.dataPath, "ASTC", "Output"));
            if (outputDir.Exists == false)
            {
                outputDir.Create();
            }

            FileInfo inputFileInfo = new FileInfo(AssetDatabase.GetAssetPath(inputObj));

            string[] fileNames = inputFileInfo.Name.Split('.');

            string outputPath = Path.Combine(outputDir.FullName, $"{fileNames[0]}.astc");

            List<string> argList = new List<string>
            {
                $"-{selectedProfile}",
                inputFileInfo.FullName,
                outputPath,
                $"{selectedBlockSize.ToString().Replace("_", "")}",
                $"-{selectedQuality}",
            };
            if (isFlip == true)
            {
                argList.Add("-yflip");
            }

            return string.Join(" ", argList);
        }

        private bool RunCommand(UnityEngine.Object file)
        {
            string path = Path.GetFullPath("Packages/ ucf.media.service/Editor/Tools/ASTC/bin/Windows");
            string fileName = "astcenc-sse4.1.exe";
            string args = CreateArgument(file);

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = Path.Combine(path, fileName),
                    Arguments = args.ToString(),
                    WorkingDirectory = path,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            StringBuilder sb = new StringBuilder();
            process.ErrorDataReceived += (sender, e) => {
                if (e.Data.Length > 0)
                {
                    sb.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Dispose();

            if (sb.Length > 0)
            {
                UnityEngine.Debug.LogError(sb.ToString());
                return false;
            }
            return true;
        }
    }
}