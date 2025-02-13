import bpy
import os
import xml.etree.ElementTree as ET
from bpy.props import StringProperty, BoolProperty, IntProperty  # Added IntProperty for scenario selection
from bpy.types import Operator, Panel, AddonPreferences

bl_info = {
    "name": "SMO Level Importer",
    "blender": (2, 93, 0),
    "category": "Import-Export",
    "description": "Import objects defined in an XML file along with their transformations and models",
    "author": "exelix, Zee",  # Authors updated
    "version": (1, 0, 0),
    "warning": "",
    "tracker_url": "",
    "support": "COMMUNITY"
}

class SMO_Level_Importer_AddonPreferences(AddonPreferences):
    bl_idname = __name__

    obj_folder: StringProperty(
        name="OBJ Folder",
        description="Folder where the .obj files are located",
        default="",
        subtype='DIR_PATH'
    )

    exclude_objects: StringProperty(
        name="Exclude Objects",
        description="Comma separated list of object names to exclude from import",
        default="",
        subtype='NONE'
    )
    
    debug_log: BoolProperty(
        name="Enable Debug Logging",
        description="Enable detailed logging for debugging purposes",
        default=False
    )
    
    selected_scenario: IntProperty(
        name="Scenario Number",
        description="Select which scenario to import (1-14)",
        default=1,
        min=1,
        max=14
    )

    def draw(self, context):
        layout = self.layout
        layout.label(text="SMO Level Importer Preferences")
        layout.prop(self, "obj_folder")
        layout.prop(self, "exclude_objects")
        layout.prop(self, "debug_log")
        layout.prop(self, "selected_scenario")  # Add scenario selection


def load_object(model, unit_config_name, x, y, z, scale_x, scale_y, scale_z, rotation_x, rotation_y, rotate_z, obj_folder, exclude_objects, debug_log):
    if debug_log:
        print(f"Loading object: {model}")
    
    if model in exclude_objects:
        if debug_log:
            print(f"Object {model} is excluded from import.")
        return

    obj_file_path = os.path.join(obj_folder, unit_config_name + ".obj")
    
    if not os.path.exists(obj_file_path):
        if debug_log:
            print(f"{unit_config_name}.obj not found, falling back to {model}.obj")
        obj_file_path = os.path.join(obj_folder, model + ".obj")
    
    if not os.path.exists(obj_file_path):
        if debug_log:
            print(f"Error: Neither {unit_config_name}.obj nor {model}.obj found!")
        return
    
    if debug_log:
        print(f"Importing OBJ: {obj_file_path}...")
    bpy.ops.import_scene.obj(filepath=obj_file_path)
    imported_obj = bpy.context.selected_objects[-1]
    
    imported_obj.location = (x, y, z)
    imported_obj.scale = (scale_x, scale_y, scale_z)
    imported_obj.rotation_euler = (rotation_x * 3.14159 / 180, rotation_y * 3.14159 / 180, rotate_z * 3.14159 / 180)


def read_xml_file(file_path, debug_log):
    if debug_log:
        print(f"Reading XML file: {file_path}")
    try:
        with open(file_path, "rb") as f:
            raw_data = f.read()

        if raw_data.startswith(b'\xff\xfe'):  
            xml_data = raw_data.decode('utf-16')
        elif raw_data.startswith(b'\xfe\xff'):  
            xml_data = raw_data.decode('utf-16-be')
        elif raw_data.startswith(b'\xef\xbb\xbf'):  
            xml_data = raw_data.decode('utf-8-sig')
        else:
            xml_data = raw_data.decode('utf-8')

        return ET.fromstring(xml_data)
    except Exception as e:
        if debug_log:
            print(f"Error reading XML file: {e}")
        return None


def read_vector3(node):
    if node is not None:
        x = float(node.find("T210[@N='X']").get("V"))
        y = float(node.find("T210[@N='Y']").get("V"))
        z = float(node.find("T210[@N='Z']").get("V"))
        return x, y, z
    return 0.0, 0.0, 0.0


def process_xml(xml_root, obj_folder, exclude_objects, debug_log, selected_scenario):
    if xml_root is None:
        if debug_log:
            print("Error: Root of XML is None")
        return

    # Explore the BymlRoot and process objects
    byml_root = xml_root.find("BymlRoot")
    if byml_root is None:
        if debug_log:
            print("Error: BymlRoot element is missing.")
        return

    scenario_root = byml_root.find("T192")
    if scenario_root is None:
        if debug_log:
            print("Error: Scenario root is missing.")
        return

    # Loop through all the scenarios and process the selected one
    for i, target_scenario in enumerate(scenario_root, start=1):
        if i == selected_scenario:  # Only process the selected scenario
            if debug_log:
                print(f"Processing scenario {i}")
            
            for obj_list in target_scenario:
                list_name = obj_list.get("N")
                if debug_log:
                    print(f"Processing object list: {list_name}")
                
                for obj in obj_list:
                    try:
                        model = obj.find("T160[@N='ModelName']")
                        unit_config_name = obj.find("T160[@N='UnitConfigName']")
                        
                        if model is not None:
                            model_name_value = model.get("V")
                        else:
                            model_name_value = None

                        if unit_config_name is not None:
                            unit_config_name_value = unit_config_name.get("V")
                        else:
                            unit_config_name_value = None

                        if not model_name_value and not unit_config_name_value:
                            if debug_log:
                                print(f"Error: Missing ModelName or UnitConfigName for object {obj}")
                            continue
                        
                        model_name_value = model_name_value or unit_config_name_value

                        (x, y, z) = read_vector3(obj.find("T193[@N='Translate']"))
                        (s_x, s_y, s_z) = read_vector3(obj.find("T193[@N='Scale']"))
                        (r_x, r_y, r_z) = read_vector3(obj.find("T193[@N='Rotate']"))

                        load_object(model_name_value, unit_config_name_value, x, y, z, s_x, s_y, s_z, r_x, r_y, r_z, obj_folder, exclude_objects, debug_log)
                        
                    except Exception as e:
                        if debug_log:
                            print(f"Error processing object: {e}")
                        continue


class SMO_Level_Importer_OT_FileSelect(Operator):
    bl_idname = "import_scene.smo_level_file_select"
    bl_label = "Select SMO Level XML File"
    filepath: StringProperty(subtype='FILE_PATH')

    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

    def execute(self, context):
        if not self.filepath:
            self.report({'ERROR'}, "No XML file selected!")
            return {'CANCELLED'}

        preferences = context.preferences.addons[__name__].preferences
        obj_folder = preferences.obj_folder
        exclude_objects = preferences.exclude_objects.split(",")
        debug_log = preferences.debug_log
        selected_scenario = preferences.selected_scenario  # Get the selected scenario

        if not obj_folder:
            self.report({'ERROR'}, "No OBJ folder path set in preferences!")
            return {'CANCELLED'}

        try:
            xml_root = read_xml_file(self.filepath, debug_log)
            if xml_root is None:
                self.report({'ERROR'}, "Failed to read XML file!")
                return {'CANCELLED'}
            
            # Now process the XML content
            process_xml(xml_root, obj_folder, exclude_objects, debug_log, selected_scenario)
        except Exception as e:
            self.report({'ERROR'}, f"Error processing XML: {str(e)}")
            return {'CANCELLED'}

        return {'FINISHED'}


class SMO_Level_Importer_PT_Panel(Panel):
    bl_label = "SMO Level Importer"
    bl_idname = "SMO_Level_Importer_PT_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "Import"

    def draw(self, context):
        layout = self.layout
        preferences = context.preferences.addons[__name__].preferences
        layout.prop(preferences, "obj_folder")
        layout.prop(preferences, "exclude_objects")
        layout.prop(preferences, "debug_log")
        layout.prop(preferences, "selected_scenario")  # Add scenario selection
        layout.operator("import_scene.smo_level_file_select", text="Import SMO Level XML")


def menu_func_import(self, context):
    self.layout.operator(SMO_Level_Importer_OT_FileSelect.bl_idname, text="Import SMO Level XML")

def register():
    bpy.utils.register_class(SMO_Level_Importer_AddonPreferences)
    bpy.utils.register_class(SMO_Level_Importer_OT_FileSelect)
    bpy.utils.register_class(SMO_Level_Importer_PT_Panel)
    bpy.types.TOPBAR_MT_file_import.append(menu_func_import)

def unregister():
    bpy.utils.unregister_class(SMO_Level_Importer_AddonPreferences)
    bpy.utils.unregister_class(SMO_Level_Importer_OT_FileSelect)
    bpy.utils.unregister_class(SMO_Level_Importer_PT_Panel)
    bpy.types.TOPBAR_MT_file_import.remove(menu_func_import)

if __name__ == "__main__":
    register()
