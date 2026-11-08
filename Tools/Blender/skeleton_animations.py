"""สร้างท่า Throw / Death / Hit ให้ Skeleton (AdvancedSkeleton rig) แล้ว export เป็น FBX ทีละท่า
แกนใน armature space: Y = ขึ้น, +Z = หน้าตัวละคร, -X = ขวาของตัวละคร
"""
import bpy, math, sys, os
from mathutils import Matrix, Vector

SRC = '/mnt/user-data/uploads/bu-2026-sem1-gi262-final/Assets/Skeleton/FBX/'
OUT = '/tmp/claude-0/skel/out/'
RENDER = '--render' in sys.argv
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)

# Skeleton.fbx (มี mesh) ใช้แค่เรนเดอร์ภาพเช็กท่า — เปลี่ยนชื่อกันชนกับของ IDLE
bpy.ops.import_scene.fbx(filepath=SRC + 'Skeleton.fbx')
render_objs = list(bpy.data.objects)
for o in render_objs:
    o.name = o.name + '_render'
render_arm = bpy.data.objects['DeformationSystem_render']
mesh_objs = [o for o in render_objs if o.type == 'MESH']

# ทำท่าบน armature ของ IDLE.fbx เอง (rest pose ตรงกับไฟล์ท่าเดิม — ทดสอบ round-trip แล้วตรง 100%)
# ห้ามเอา action ไปใส่ armature ของ Skeleton.fbx: rest pose ไม่เหมือนกัน ค่าที่ key ไว้จะเพี้ยน
bpy.ops.import_scene.fbx(filepath=SRC + 'IDLE.fbx')
arm = bpy.data.objects['DeformationSystem']
idle_action = [a for a in bpy.data.actions if 'IDLE' in a.name][0]

scene = bpy.context.scene
scene.render.fps = 30
if arm.animation_data is None:
    arm.animation_data_create()
arm.animation_data.action = idle_action
if hasattr(arm.animation_data, 'action_slot') and arm.animation_data.action_slot is None and len(idle_action.slots):
    arm.animation_data.action_slot = idle_action.slots[0]
scene.frame_set(int(idle_action.frame_range[0]))
bpy.context.view_layer.update()
for pb in arm.pose.bones:
    pb.rotation_mode = 'QUATERNION'
base = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}
GROUND = 0.0
arm.animation_data.action = None


def reset_pose():
    for pb in arm.pose.bones:
        pb.matrix_basis = base[pb.name]
    bpy.context.view_layer.update()


AX = {'x': Vector((1, 0, 0)), 'y': Vector((0, 1, 0)), 'z': Vector((0, 0, 1))}


def rot(bone, axis, deg):
    """หมุนกระดูกรอบแกนของ armature (ไม่ใช่แกนของกระดูก) โดยใช้หัวกระดูกเป็นจุดหมุน"""
    pb = arm.pose.bones[bone]
    pivot = pb.head.copy()
    r = Matrix.Rotation(math.radians(deg), 4, AX[axis])
    pb.matrix = Matrix.Translation(pivot) @ r @ Matrix.Translation(-pivot) @ pb.matrix
    bpy.context.view_layer.update()


def move(bone, x=0, y=0, z=0):
    """เลื่อนกระดูกในแกน armature (หน่วย cm)"""
    pb = arm.pose.bones[bone]
    pb.matrix = Matrix.Translation(Vector((x, y, z))) @ pb.matrix
    bpy.context.view_layer.update()


def aim(bone, child, x, y, z):
    """หมุนกระดูกให้ชี้ไปทิศ (x,y,z) ใน armature space — ใช้กับแขน/ขา เพราะท่ายืนของตัวนี้หลังค่อม หมุนรอบแกนตรง ๆ แล้วไม่ไปทางที่ต้องการ"""
    pb = arm.pose.bones[bone]
    cur = (arm.pose.bones[child].head - pb.head).normalized()
    q = cur.rotation_difference(Vector((x, y, z)).normalized())
    pivot = pb.head.copy()
    pb.matrix = Matrix.Translation(pivot) @ q.to_matrix().to_4x4() @ Matrix.Translation(-pivot) @ pb.matrix
    bpy.context.view_layer.update()


def sync_render():
    """ก๊อปท่าจาก armature ที่ทำงานอยู่ไปให้ armature ของ mesh (เทียบตำแหน่งใน world ทีละกระดูก พ่อก่อนลูก)"""
    to_render = render_arm.matrix_world.inverted() @ arm.matrix_world
    for b in render_arm.data.bones:   # data.bones เรียงพ่อก่อนลูกอยู่แล้ว
        if b.name in arm.pose.bones:
            render_arm.pose.bones[b.name].matrix = to_render @ arm.pose.bones[b.name].matrix
            bpy.context.view_layer.update()


def lowest_z():
    """จุดต่ำสุดของ mesh จริง (world) หลัง deform"""
    sync_render()
    dg = bpy.context.evaluated_depsgraph_get()
    low = 1e9
    for m in mesh_objs:
        ev = m.evaluated_get(dg)
        me = ev.to_mesh()
        mw = ev.matrix_world
        for v in me.vertices:
            low = min(low, (mw @ v.co).z)
        ev.to_mesh_clear()
    return low


def to_ground():
    """เลื่อน Root ขึ้น/ลงให้ mesh แตะพื้นพอดี (พื้น = จุดต่ำสุดตอนยืน)"""
    dz = GROUND - lowest_z()
    scale = arm.matrix_world.to_scale().x  # armature หน่วย cm -> world m
    move('Root_M', 0, dz / scale, 0)


def key_all(frame):
    for pb in arm.pose.bones:
        pb.keyframe_insert('rotation_quaternion', frame=frame)
        pb.keyframe_insert('location', frame=frame)


def make_clip(name, poses):
    """poses = [(frame, ฟังก์ชันจัดท่า), ...] เรียงตามเฟรม"""
    action = bpy.data.actions.new(name)
    arm.animation_data.action = action
    for frame, pose in poses:
        reset_pose()
        pose()
        key_all(frame)
    # ทำให้ท่านุ่ม (Bezier) ตลอดคลิป
    curves = []
    if hasattr(action, 'fcurves'):
        curves = list(action.fcurves)
    else:
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    curves += list(bag.fcurves)
    for fc in curves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'
    return action


reset_pose()
GROUND = lowest_z()
print('GROUND', GROUND)

# ---------- ท่าปาหิน (ขวามือ) — ปล่อยหินที่เฟรม 15 จาก 33 (~0.45) ----------
def throw_idle():
    pass

def throw_windup():
    move('Root_M', 0, -4, -3)
    rot('Root_M', 'y', -15)
    rot('Spine1_M', 'x', -6)
    rot('Chest_M', 'y', -25)
    rot('Chest_M', 'x', -6)
    aim('Shoulder_R', 'Elbow_R', -0.55, 0.25, -0.8)   # ศอกขวาง้างไปข้างหลัง
    aim('Elbow_R', 'Wrist_R', -0.15, 0.85, -0.5)      # มือถือหินชูเหนือหัว
    aim('Shoulder_L', 'Elbow_L', 0.35, -0.05, 0.95)   # แขนซ้ายยื่นหน้าถ่วงตัว
    aim('Elbow_L', 'Wrist_L', 0.3, 0.1, 0.95)
    rot('Head_M', 'y', 15)

def throw_peak():
    move('Root_M', 0, -5, -5)
    rot('Root_M', 'y', -18)
    rot('Spine1_M', 'x', -8)
    rot('Chest_M', 'y', -30)
    rot('Chest_M', 'x', -8)
    aim('Shoulder_R', 'Elbow_R', -0.5, 0.35, -0.8)
    aim('Elbow_R', 'Wrist_R', -0.05, 0.8, -0.6)
    aim('Shoulder_L', 'Elbow_L', 0.35, 0.05, 0.95)
    aim('Elbow_L', 'Wrist_L', 0.3, 0.2, 0.95)
    rot('Head_M', 'y', 18)

def throw_release():
    move('Root_M', 0, -3, 6)
    rot('Root_M', 'y', 12)
    rot('Spine1_M', 'x', 3)
    rot('Chest_M', 'y', 22)
    rot('Chest_M', 'x', 4)
    aim('Shoulder_R', 'Elbow_R', -0.25, 0.5, 0.85)    # เหวี่ยงไปหน้า ปล่อยหิน
    aim('Elbow_R', 'Wrist_R', -0.1, 0.45, 0.9)
    aim('Shoulder_L', 'Elbow_L', 0.45, -0.6, -0.65)   # แขนซ้ายสะบัดไปหลัง
    aim('Elbow_L', 'Wrist_L', 0.4, -0.7, -0.6)
    rot('Head_M', 'x', -22)   # เงยหน้ามองเป้า

def throw_follow():
    move('Root_M', 0, -4, 8)
    rot('Root_M', 'y', 18)
    rot('Spine1_M', 'x', 6)
    rot('Chest_M', 'y', 28)
    rot('Chest_M', 'x', 8)
    rot('Head_M', 'x', -15)
    aim('Shoulder_R', 'Elbow_R', -0.1, -0.45, 0.9)    # ส่งแขนตามลงล่าง
    aim('Elbow_R', 'Wrist_R', 0.05, -0.7, 0.7)
    aim('Shoulder_L', 'Elbow_L', 0.45, -0.7, -0.55)
    aim('Elbow_L', 'Wrist_L', 0.4, -0.8, -0.4)

throw = make_clip('Throw', [(1, throw_idle), (8, throw_windup), (12, throw_peak), (15, throw_release),
                            (21, throw_follow), (33, throw_idle)])


# ---------- ท่าตาย (ล้มหงายหลัง) 40 เฟรม ค้างท่าสุดท้าย ----------
def die_idle():
    pass

def die_impact():
    rot('Chest_M', 'x', -18)
    rot('Head_M', 'x', -25)
    rot('Shoulder_R', 'x', -35)
    rot('Shoulder_L', 'x', -35)
    move('Root_M', 0, -2, -4)

def die_buckle():
    move('Root_M', 0, -28, -6)
    rot('Root_M', 'x', -12)
    rot('Hip_R', 'x', -55)
    rot('Hip_L', 'x', -45)
    rot('Knee_R', 'x', 95)
    rot('Knee_L', 'x', 85)
    rot('Chest_M', 'x', 18)
    rot('Head_M', 'x', 25)
    rot('Shoulder_R', 'x', -20)
    rot('Shoulder_L', 'x', -25)
    to_ground()

def die_falling():
    move('Root_M', 0, -52, -22)
    rot('Root_M', 'x', -55)
    aim('Hip_R', 'Knee_R', -0.2, -0.75, 0.6)
    aim('Knee_R', 'Ankle_R', -0.2, -0.95, 0.2)
    aim('Hip_L', 'Knee_L', 0.2, -0.7, 0.65)
    aim('Knee_L', 'Ankle_L', 0.2, -0.9, 0.35)
    rot('Head_M', 'x', 15)
    aim('Shoulder_R', 'Elbow_R', -0.5, 0.6, 0.6)     # แขนสะบัดขึ้นตอนล้ม
    aim('Elbow_R', 'Wrist_R', -0.4, 0.8, 0.4)
    aim('Shoulder_L', 'Elbow_L', 0.5, 0.55, 0.65)
    aim('Elbow_L', 'Wrist_L', 0.4, 0.8, 0.45)
    to_ground()

def die_ground():
    move('Root_M', 0, -60, -40)
    rot('Root_M', 'x', -86)                          # นอนหงาย ลำตัวราบไปด้านหลัง
    aim('Hip_R', 'Knee_R', -0.2, -0.06, 0.98)        # ขาราบไปกับพื้น
    aim('Knee_R', 'Ankle_R', -0.22, -0.05, 0.97)
    aim('Hip_L', 'Knee_L', 0.25, -0.02, 0.97)
    aim('Knee_L', 'Ankle_L', 0.32, -0.08, 0.94)
    rot('Head_M', 'y', 25)
    aim('Shoulder_R', 'Elbow_R', -0.92, -0.05, -0.38)  # แขนกางแผ่บนพื้น
    aim('Elbow_R', 'Wrist_R', -0.7, -0.05, -0.7)
    aim('Shoulder_L', 'Elbow_L', 0.95, -0.05, -0.2)
    aim('Elbow_L', 'Wrist_L', 0.8, -0.05, 0.55)
    to_ground()

def die_bounce():
    die_ground()
    move('Root_M', 0, 5, 0)   # เด้งขึ้นนิดนึงตอนกระแทกพื้น

death = make_clip('Death', [(1, die_idle), (5, die_impact), (13, die_buckle), (22, die_falling),
                            (29, die_ground), (33, die_bounce), (37, die_ground), (40, die_ground)])


# ---------- ท่าโดนตี (สะดุ้งถอยหลัง) 14 เฟรม ----------
def hit_idle():
    pass

def hit_recoil():
    move('Root_M', 0, -3, -6)
    rot('Spine1_M', 'x', -10)
    rot('Chest_M', 'x', -14)
    rot('Head_M', 'x', -20)
    rot('Shoulder_R', 'x', -25)
    rot('Shoulder_L', 'x', -30)

hit = make_clip('Hit', [(1, hit_idle), (4, hit_recoil), (14, hit_idle)])


# ---------- export ทีละท่า (เฉพาะกระดูก ไม่เอา mesh) ----------
def export_clip(action, filename):
    arm.animation_data.action = action
    scene.name = action.name   # ชื่อ take ใน FBX = ชื่อคลิปใน Unity
    s, e = action.frame_range
    scene.frame_start, scene.frame_end = int(s), int(e)
    bpy.ops.export_scene.fbx(filepath=OUT + filename, use_selection=False, object_types={'ARMATURE', 'EMPTY'},
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False,
                             bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0,
                             apply_scale_options='FBX_SCALE_NONE', axis_forward='-Z', axis_up='Y')

if not RENDER:
    # ลบ empty ฝั่ง mesh ทิ้งก่อน export ให้เหลือแค่ Group/DeformationSystem/กระดูก เหมือน IDLE.fbx
    for o in render_objs:
        bpy.data.objects.remove(o, do_unlink=True)
    export_clip(throw, 'Skeleton_Throw.fbx')
    export_clip(death, 'Skeleton_Death.fbx')
    export_clip(hit, 'Skeleton_Hit.fbx')
    print('EXPORTED')
else:
    # เรนเดอร์ภาพเช็กท่า: กล้องมุมข้าง-หน้า, Workbench
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 360, 420
    cam_data = bpy.data.cameras.new('cam')
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 2.6
    cam = bpy.data.objects.new('cam', cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    # world space (Z ขึ้น หลังแปลง Group): ตัวละครหันไป -Y ของ Blender
    # หาขอบเขตตัวละครจริงใน world แล้วเล็งกล้องไปกลางตัว (มองจากหน้า-ขวา 45 องศา)
    import mathutils
    pts = [m.matrix_world @ Vector(c) for m in mesh_objs for c in m.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    print('BOUNDS', lo, hi)
    center = (lo + hi) / 2
    # ทิศหน้าตัวละครใน world = แกน +Z ของ armature
    fwd = (arm.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()
    right = (arm.matrix_world.to_3x3() @ Vector((-1, 0, 0))).normalized()
    up = (arm.matrix_world.to_3x3() @ Vector((0, 1, 0))).normalized()
    print('FWD', fwd, 'UP', up)
    view = right if '--side' in sys.argv else (fwd + right * 0.9).normalized()
    cam.location = center + view * 6
    cam.rotation_euler = (-view).to_track_quat('-Z', 'Y').to_euler()
    cam_data.ortho_scale = 3.2
    bpy.ops.mesh.primitive_plane_add(size=6, location=(center.x, center.y, GROUND))
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.show_shadows = True
    for name, action, frames in [('throw', throw, [1, 8, 12, 15, 21, 33]),
                                 ('death', death, [1, 5, 13, 22, 29, 40]),
                                 ('hit', hit, [1, 4, 14])]:
        arm.animation_data.action = action
        for f in frames:
            scene.frame_set(f)
            sync_render()
            scene.render.filepath = f'/tmp/claude-0/skel/render/{name}_{f:02d}.png'
            bpy.ops.render.render(write_still=True)
    print('RENDERED')
