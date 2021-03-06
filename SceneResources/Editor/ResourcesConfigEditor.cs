﻿using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace WZK
{
    [CustomEditor(typeof(ResourcesConfig))]
    public class ResourcesConfigEditor : Editor
    {
        private string _directionPath;//文件夹路径
        private string _fileAssetPath;//文件工程目录
        private bool _isExist;//是否已存在
        private bool _isDelete;//删除资源
        public List<string> _extensionList = new List<string> { ".mp3", ".ogg", ".asset", ".txt", ".xml", ".mat", ".prefab", ".png", ".jpg" };//选择的扩展名列表
        public override void OnInspectorGUI()
        {
            ResourcesConfig resourcesConfig = target as ResourcesConfig;
            int index = -1;
            for (int i = 0; i < _extensionList.Count; i++)
            {
                if (i % 4 == 0) EditorGUILayout.BeginHorizontal();
                index = resourcesConfig._choseExtensionList.IndexOf(_extensionList[i]);
                if (GUILayout.Button(_extensionList[i]))
                {
                    if (index == -1)
                    {
                        resourcesConfig._choseExtensionList.Add(_extensionList[i]);
                    }
                    else
                    {
                        resourcesConfig._choseExtensionList.RemoveAt(index);
                    }
                }
                if ((i > 1 && i % 4 == 3) || i == _extensionList.Count - 1) EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
            if (resourcesConfig._choseExtensionList.Count == 0)
            {
                EditorGUILayout.LabelField("没有选择指定的后缀，默认包含以上所有后缀！");

            }
            else
            {
                string str = "";
                for (int i = 0; i < resourcesConfig._choseExtensionList.Count; i++)
                {
                    str += resourcesConfig._choseExtensionList[i];
                    if (i < resourcesConfig._choseExtensionList.Count - 1)
                    {
                        str += "、";
                    }
                }
                EditorGUILayout.LabelField("选择的后缀:" + str);
            }
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("显示区间");
            resourcesConfig._showMin = EditorGUILayout.IntField(resourcesConfig._showMin);
            if (resourcesConfig._showMin < 1) resourcesConfig._showMin = 1;
            GUILayout.Label("~");
            resourcesConfig._showMax = EditorGUILayout.IntField(resourcesConfig._showMax);
            if (resourcesConfig._showMax < resourcesConfig._showMin) resourcesConfig._showMax = resourcesConfig._showMin;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            List<ResourcesConfig.Config> objList = resourcesConfig._objectList;
            for (int i = 0; i < objList.Count; i++)
            {
                if (i >= resourcesConfig._showMin - 1 && i < resourcesConfig._showMax)
                {
                    EditorGUILayout.BeginHorizontal();
                    objList[i]._object = (Object)EditorGUILayout.ObjectField("对象" + (i + 1), objList[i]._object, typeof(Object), false);
                    _isDelete = false;
                    if (GUILayout.Button("删除" + (i + 1)))
                    {
                        _isDelete = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    objList[i]._assetPath = EditorGUILayout.TextField("路径" + (i + 1), objList[i]._assetPath);
                    GUILayout.Space(10);
                    if (objList[i]._assetPath == "" && objList[i]._object) objList[i]._assetPath = objList[i]._object.name;
                    if (_isDelete) objList.RemoveAt(i);
                }
            }
            if (Event.current.type == EventType.DragExited)
            {
                System.Type type = DragAndDrop.objectReferences[0].GetType();
                if (type != typeof(DefaultAsset))
                {
                    AddObject(objList, DragAndDrop.objectReferences[0], DragAndDrop.paths[0]);
                }
                else
                {
                    _directionPath = Application.dataPath;
                    _directionPath = _directionPath.Substring(0, _directionPath.LastIndexOf("/") + 1) + DragAndDrop.paths[0];
                    if (Directory.Exists(_directionPath))
                    {
                        DirectoryInfo direction = new DirectoryInfo(_directionPath);
                        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (resourcesConfig._choseExtensionList.Count == 0 && _extensionList.Contains(Path.GetExtension(files[i].FullName)) == false)
                            {
                                continue;
                            }
                            else if (resourcesConfig._choseExtensionList.Count > 0 && resourcesConfig._choseExtensionList.Contains(Path.GetExtension(files[i].FullName)) == false)
                            {
                                continue;
                            }
                            _fileAssetPath = files[i].DirectoryName;
                            _fileAssetPath = _fileAssetPath.Substring(_fileAssetPath.IndexOf("Assets")) + "/" + files[i].Name;
                            AddObject(objList, AssetDatabase.LoadAssetAtPath<Object>(_fileAssetPath), _fileAssetPath);
                        }
                    }
                }
            }
            GUILayout.Space(30);
            if (GUILayout.Button("清空") && EditorUtility.DisplayDialog("警告", "确定要清空所有数据吗", "确定", "取消"))
            {
                resourcesConfig._objectList.Clear();
            }
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(resourcesConfig);
        }
        /// <summary>
        /// 添加音频
        /// </summary>
        private void AddObject(List<ResourcesConfig.Config> objList, Object obj, string assetPath)
        {
            //Sprite处理
            if (assetPath.Contains(".png") || assetPath.Contains(".jpg"))
            {
                Object[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                if (objects.Length >= 2)
                {
                    string tempPath = assetPath;
                    for (int i = 1; i < objects.Length; i++)
                    {
                        assetPath = tempPath.Substring(0, tempPath.LastIndexOf("/") + 1) + objects[i].name + tempPath.Substring(tempPath.IndexOf("."));
                        JudgeExist(objList, objects[i], assetPath);
                    }
                    return;
                }
            }
            JudgeExist(objList, obj, assetPath);
        }
        private void JudgeExist(List<ResourcesConfig.Config> objList, Object obj, string assetPath)
        {
            _isExist = false;
            assetPath = assetPath.Replace("\\", "/");
            for (int i = 0; i < objList.Count; i++)
            {
                if (objList[i]._object == obj)
                {
                    _isExist = true;
                    objList[i]._assetPath = assetPath;//如果有移动更新最新的地址
                    Debug.LogError("配置表里已存在该对象");
                    break;
                }
            }
            if (_isExist == false) objList.Add(new ResourcesConfig.Config(obj, assetPath));
        }
        [MenuItem("GameObject/WZK/创建场景资源管理对象", false, 19)]
        private static void CreateSoundManagerObject()
        {
            GameObject gameObject = new GameObject("场景资源管理");
            gameObject.AddComponent<SceneResources>();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = gameObject;
            EditorGUIUtility.PingObject(Selection.activeObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create GameObject");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
