using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

/* 
    Create Polygon mesh with path
 */
[CustomEditor(typeof(PolygonMesh))]
public class PolygonMeshEditor : Editor {
    PolygonMesh polygon;
    SelectionInfo info;
    float diskRadius = 0.2f;
    /* ugly fields*/
    SerializedProperty showMeshProperty;
    SerializedProperty enableHeightProperty;
    bool oldShowMesh;
    bool oldEnableHeight;

    public override void OnInspectorGUI() {
        EditorGUILayout.HelpBox("Left: Add\nRight: Remove", MessageType.Info);
        DrawDefaultInspector();

        serializedObject.Update();
        if (oldShowMesh != showMeshProperty.boolValue) {
            oldShowMesh = showMeshProperty.boolValue;
            polygon.UpdateShowMesh();
        }
        if (oldEnableHeight != enableHeightProperty.boolValue) {
            oldEnableHeight = enableHeightProperty.boolValue;
            polygon.UpdateMesh();
        }
    }

    void OnSceneGUI() {
        Event e = Event.current;
        if (e.type == EventType.Layout) {

            // focus control
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        } else if (e.type == EventType.Repaint) {
            for (int i = 0; i < polygon.points.Count; i++) {
                // LINE
                Vector3 nextPoint = polygon.points[(i + 1) % polygon.points.Count];
                Handles.color = (info.isHoveringLine && info.lineId == i) ? Color.red : Color.green;
                Handles.DrawLine(polygon.points[i], nextPoint);

                // POINT
                if (info.isHoveringPoint && info.pointId == i) {
                    Handles.color = info.isSelectingPoint ? Color.red : Color.green;
                } else {
                    Handles.color = Color.white;
                }
                Handles.DrawSolidDisc(polygon.points[i], Vector3.up, diskRadius);
            }
        } else {
            HandleMouseInputs(e);
        }
    }

    private Vector2 getXZ(Vector3 v3) {
        return new Vector2(v3.x, v3.z);
    }

    void HandleMouseInputs(Event e) {
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        // pos+dir*dst=point, dst = (h-pos.y)/dir.y
        float dst = (polygon.y - mouseRay.origin.y) / mouseRay.direction.y;
        Vector3 pointPos = mouseRay.origin + mouseRay.direction * dst;
        // update diskrad
        Ray cameraRay = HandleUtility.GUIPointToWorldRay(new Vector2(0.5f, 0.5f));
        float cameraDst = cameraRay.origin.y - polygon.y;
        diskRadius = Mathf.Clamp(Mathf.Pow(cameraDst / 20, 2), 0.2f, 3f);
        // update hovering
        info.isHoveringLine = false;
        if (!info.isSelectingPoint) {
            info.isHoveringPoint = false;
            for (int i = 0; i < polygon.points.Count; i++) {
                if (Vector3.Distance(pointPos, polygon.points[i]) < diskRadius) {
                    info.isHoveringPoint = true;
                    info.pointId = i;
                    break;
                }
            }

            if (!info.isHoveringPoint) {
                float dstline = diskRadius;
                for (int i = 0; i < polygon.points.Count; i++) {
                    Vector3 nextPoint = polygon.points[(i + 1) % polygon.points.Count];
                    float tmpDst = HandleUtility.DistancePointToLineSegment(getXZ(pointPos), getXZ(polygon.points[i]), getXZ(nextPoint));
                    if (tmpDst < dstline) {
                        dstline = tmpDst;
                        info.isHoveringLine = true;
                        info.lineId = i;
                    }
                }
            }
        }

        // MOUSE EVENTS
        if (e.type == EventType.MouseDown && e.button == 0 && e.modifiers == EventModifiers.None) {
            if (!info.isHoveringPoint) {
                Undo.RecordObject(polygon, "Add Point");
                int addId = info.isHoveringLine ? info.lineId + 1 : polygon.points.Count;
                polygon.points.Insert(addId, pointPos);
                info.pointId = addId;
                polygon.UpdateMesh();
            }
            info.isSelectingPoint = true;
            info.dragStartPos = pointPos;
        } else if (e.type == EventType.MouseUp && e.button == 0 && e.modifiers == EventModifiers.None) {
            if (info.isSelectingPoint) {
                polygon.points[info.pointId] = info.dragStartPos;
                Undo.RecordObject(polygon, "Move Point");
                polygon.points[info.pointId] = pointPos;
                info.isSelectingPoint = false;
                polygon.UpdateMesh();
            }
        } else if (e.type == EventType.MouseDrag && e.button == 0 && e.modifiers == EventModifiers.None) {
            if (info.isSelectingPoint) {
                polygon.points[info.pointId] = pointPos;
            }
        } else if (e.type == EventType.MouseDown && e.button == 1 && e.modifiers == EventModifiers.None) {
            if (info.isHoveringPoint) {
                Undo.RecordObject(polygon, "Delete Point");
                polygon.points.RemoveAt(info.pointId);
                polygon.UpdateMesh();
            }

        }

    }

    void OnEnable() {
        showMeshProperty = serializedObject.FindProperty("showMesh");
        oldShowMesh = showMeshProperty.boolValue;
        enableHeightProperty = serializedObject.FindProperty("enableHeight");
        oldEnableHeight = enableHeightProperty.boolValue;

        polygon = target as PolygonMesh;
        info = new SelectionInfo();
        Tools.hidden = true;
        Undo.undoRedoPerformed += polygon.UpdateMesh;
    }

    void OnDisable() {
        Tools.hidden = false;
        Undo.undoRedoPerformed -= polygon.UpdateMesh;
    }

    public class SelectionInfo {
        public int pointId = -1;
        public int lineId = -1;
        public bool isHoveringPoint = false;
        public bool isHoveringLine = false;
        public bool isSelectingPoint = false;
        public Vector3 dragStartPos = Vector3.zero;

    }
}
