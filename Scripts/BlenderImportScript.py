import json
import bpy
import os
import math
import mathutils

def find_layer_collection(coll, find):
    for c in coll.children:
        if c.collection == find:
            return c
    return None

def has_child(coll, name):
    for child in coll.children:
        if child.name == name:
            return True
    return False

def create_obj_instance(modelName, modelsColl, instColl, instName):
    obj = bpy.data.objects.new(instName, None)
    instColl.objects.link(obj)
    
    obj.instance_collection = modelsColl.children.get(modelName)
    obj.instance_type = 'COLLECTION'
    
    return obj
    
def get_or_create_collection(name):
    coll = bpy.data.collections.get(name)
    if coll is not None:
        return coll
    coll = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)
    
    return coll


workingDir = "F:\\Users\\Talib\\Downloads\\ModdingStuff\\VSProjects\\Sample-Editor\\Test Saves\\CityWorldHomeStage" # AnimalChaseExStage
stageName = os.path.basename(workingDir)


print("Loading Data for stage: " + stageName)

with open(os.path.join(workingDir, stageName + ".json"), 'r') as file:
    jsonString = file.read()

positionData = json.loads(jsonString)

origCollection = bpy.context.view_layer.active_layer_collection
prevPivot = bpy.context.scene.tool_settings.transform_pivot_point
cursor_location = bpy.context.scene.cursor.location
prevCtx = bpy.context.area.type

# Set custom context data
#bpy.context.scene.tool_settings.transform_pivot_point = 'MEDIAN_POINT'
#bpy.context.area.type = 'VIEW_3D' # setting this causes blender to crash when importing models (crashes even if this is set after imports)
#bpy.context.scene.cursor.location = (0, 0, 0)

# Create Instance collection 
modelsColl = get_or_create_collection("Models")
instCollection = get_or_create_collection("Instances")

modelCollLayer = find_layer_collection(bpy.context.view_layer.layer_collection, modelsColl)
InstCollLayer = find_layer_collection(bpy.context.view_layer.layer_collection, instCollection)

for modelName in positionData["ExportedModels"]:
    if has_child(modelsColl, modelName):
        continue
    
    collection = bpy.data.collections.new(modelName)
    modelsColl.children.link(collection)
    bpy.context.view_layer.active_layer_collection = find_layer_collection(modelCollLayer, collection)

    bpy.ops.wm.collada_import(filepath = os.path.join(workingDir, modelName, modelName + ".dae"))

modelCollLayer.exclude = True

#bpy.ops.object.select_all(action='DESELECT')

for collectionName, placementInfo in positionData["PlacementInfo"].items():
    archiveName = collectionName[:collectionName.index('_')]
    
    print("\tArchive Folder Name: " + archiveName)

    obj = create_obj_instance(archiveName, modelsColl, instCollection, collectionName)

    objPosition = (placementInfo["Position"]["X"], -placementInfo["Position"]["Z"], placementInfo["Position"]["Y"])
    objRotation = (math.radians(placementInfo["Rotation"]["X"]), math.radians(-placementInfo["Rotation"]["Z"]), math.radians(placementInfo["Rotation"]["Y"]))
    objScale = (placementInfo["Scale"]["X"], placementInfo["Scale"]["Z"], placementInfo["Scale"]["Y"])                    
    
    obj.location = objPosition
    obj.scale = objScale
    obj.rotation_euler.rotate_axis("X", objRotation[0])
    obj.rotation_euler.rotate_axis("Y", objRotation[1])
    obj.rotation_euler.rotate_axis("Z", objRotation[2])
#    obj.select_set(True)

# scale all instanced objects down so they're nicer to view
#bpy.ops.transform.resize(value=(0.001, 0.001, 0.001))

#bpy.context.scene.tool_settings.transform_pivot_point = prevPivot
#bpy.context.scene.cursor.location = cursor_location

bpy.context.view_layer.active_layer_collection = origCollection