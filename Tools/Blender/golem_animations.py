"""ท่าของโกเลม (Kevin Iglesias Humanoid Giant) — ไฟล์โมเดลไม่มีท่ามาเลย ทำใหม่ทั้งหมด 6 ท่า
Idle / Walk (วนลูป) , Slam (ทุ่มพื้น) , Throw (ปาหิน) , Hit , Death
แกน armature: Y = ขึ้น, +Z = หน้า, +X = ซ้ายของตัวละคร (-X = ขวา)  หน่วย cm (สูง ~2.5 m)
หมายเหตุ: Blender 5.2 import ไฟล์นี้ไม่ได้ เพราะ B-hips เป็นโหนดชนิด "Root" -> ต้องแก้ import_fbx.py ให้รับ b'Root' เป็นกระดูกด้วย
"""
import bpy, math, sys, os
from mathutils import Matrix, Vector

SRC = '/mnt/user-data/uploads/bu-2026-sem1-gi262-final/Assets/Kevin Iglesias/Characters/Humanoid Giant/Models/GiantModel01 - Golem.fbx'
OUT = '/tmp/claude-0/skel/golem_out/'
RENDER = '--render' in sys.argv
SIDE = '--side' in sys.argv
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
arm = bpy.data.objects['B-root']
mesh_objs = [o for o in bpy.data.objects if o.type == 'MESH']
scene = bpy.context.scene
scene.render.fps = 30
arm.animation_data_create()
for pb in arm.pose.bones:
    pb.rotation_mode = 'QUATERNION'
    pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()

AX = {'x': Vector((1, 0, 0)), 'y': Vector((0, 1, 0)), 'z': Vector((0, 0, 1))}


def upd():
    bpy.context.view_layer.update()


def reset_pose():
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    upd()


def rot(bone, axis, deg):
    pb = arm.pose.bones[bone]
    pivot = pb.head.copy()
    pb.matrix = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(deg), 4, AX[axis]) @ Matrix.Translation(-pivot) @ pb.matrix
    upd()


def move(bone, x=0, y=0, z=0):
    pb = arm.pose.bones[bone]
    pb.matrix = Matrix.Translation(Vector((x, y, z))) @ pb.matrix
    upd()


def aim(bone, child, x, y, z):
    pb = arm.pose.bones[bone]
    cur = (arm.pose.bones[child].head - pb.head).normalized()
    q = cur.rotation_difference(Vector((x, y, z)).normalized())
    pivot = pb.head.copy()
    pb.matrix = Matrix.Translation(pivot) @ q.to_matrix().to_4x4() @ Matrix.Translation(-pivot) @ pb.matrix
    upd()


def lowest_z():
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


def to_ground(extra=0.0):
    dz = GROUND - lowest_z() + extra
    move('B-hips', 0, dz / arm.matrix_world.to_scale().x, 0)


def key_all(frame):
    for pb in arm.pose.bones:
        pb.keyframe_insert('rotation_quaternion', frame=frame)
        pb.keyframe_insert('location', frame=frame)


def make_clip(name, poses):
    action = bpy.data.actions.new(name)
    arm.animation_data.action = action
    for frame, pose in poses:
        reset_pose()
        pose()
        key_all(frame)
    return action


# ---------- ท่ายืนตั้งต้นของโกเลม: หลังค่อม แขนห้อยหนัก ๆ เข่างอนิด ----------
def arms_hang(sway_r=0.0, sway_l=0.0):
    aim('B-upperArm.R', 'B-forearm.R', -0.42, -0.9, 0.12 + sway_r)
    aim('B-forearm.R', 'B-hand.R', -0.25, -0.9, 0.35 + sway_r)
    aim('B-upperArm.L', 'B-forearm.L', 0.42, -0.9, 0.12 + sway_l)
    aim('B-forearm.L', 'B-hand.L', 0.25, -0.9, 0.35 + sway_l)


def stance(breath=0.0):
    rot('B-spine', 'x', 8)
    rot('B-chest', 'x', 6 + breath)
    rot('B-neck', 'x', -10)
    arms_hang()
    aim('B-thigh.R', 'B-shin.R', -0.08, -0.98, 0.18)   # เข่างอนิด ยืนกางขา
    aim('B-shin.R', 'B-foot.R', -0.02, -0.98, -0.1)
    aim('B-thigh.L', 'B-shin.L', 0.08, -0.98, 0.18)
    aim('B-shin.L', 'B-foot.L', 0.02, -0.98, -0.1)


reset_pose()
stance()
GROUND = lowest_z()
reset_pose()
GROUND_T = lowest_z()   # ท่า T-pose (เท้าแตะพื้นจริง)
GROUND = GROUND_T
print('GROUND', GROUND)


def st():
    stance(); to_ground()


# ---------- 1. Idle 60 เฟรม (วนลูป) หายใจ แขนแกว่งนิด ๆ ----------
def idle_a():
    stance(0); to_ground()

def idle_b():
    rot('B-spine', 'x', 8)
    rot('B-chest', 'x', 3)
    rot('B-neck', 'x', -8)
    arms_hang(0.05, -0.04)
    aim('B-thigh.R', 'B-shin.R', -0.08, -0.98, 0.2)
    aim('B-shin.R', 'B-foot.R', -0.02, -0.98, -0.12)
    aim('B-thigh.L', 'B-shin.L', 0.08, -0.98, 0.2)
    aim('B-shin.L', 'B-foot.L', 0.02, -0.98, -0.12)
    to_ground()

idle = make_clip('Golem_Idle', [(1, idle_a), (31, idle_b), (61, idle_a)])


# ---------- 2. Walk 40 เฟรม (วนลูป) ก้าวหนัก ๆ ----------
def walk_pose(phase):
    # phase 0 = ขาขวาหน้า, 1 = ขาซ้ายหน้า (ตอนเท้าแตะพื้น) / 0.5,1.5 = ช่วงยกขาผ่าน
    s = math.cos(phase * math.pi)            # +1 ขวาหน้า, -1 ซ้ายหน้า
    lift = abs(math.sin(phase * math.pi))    # ช่วงขาผ่าน ยกตัว
    rot('B-hips', 'y', 6 * s)                # สะโพกบิดตามก้าว
    rot('B-spine', 'x', 10)
    rot('B-chest', 'y', -9 * s)              # อกบิดสวน
    rot('B-chest', 'x', 6)
    rot('B-neck', 'x', -10)
    # ขา: ขาหน้าเหยียดไปหน้า ขาหลังเหยียดไปหลัง ขาที่ยกผ่านงอเข่า
    fr, fl = 0.45 * s, -0.45 * s
    aim('B-thigh.R', 'B-shin.R', -0.08, -0.95, fr + 0.15 * lift * (1 if s < 0 else 0))
    aim('B-shin.R', 'B-foot.R', -0.02, -0.95, fr - 0.45 * lift * (1 if s < 0.2 else 0))
    aim('B-thigh.L', 'B-shin.L', 0.08, -0.95, fl + 0.15 * lift * (1 if s > 0 else 0))
    aim('B-shin.L', 'B-foot.L', 0.02, -0.95, fl - 0.45 * lift * (1 if s > -0.2 else 0))
    # แขนแกว่งสวนขา
    arms_hang(-0.35 * s, 0.35 * s)
    to_ground()

walk = make_clip('Golem_Walk', [(1, lambda: walk_pose(0)), (11, lambda: walk_pose(0.5)), (21, lambda: walk_pose(1)),
                                (31, lambda: walk_pose(1.5)), (41, lambda: walk_pose(2))])


# ---------- 3. Slam ทุ่มพื้น 45 เฟรม — กำปั้นกระแทกพื้นที่เฟรม 24 (~0.53) ----------
def slam_raise():
    rot('B-spine', 'x', -6)
    rot('B-chest', 'x', -10)
    rot('B-neck', 'x', -5)
    aim('B-upperArm.R', 'B-forearm.R', -0.25, 0.9, -0.3)    # ชูสองแขนเหนือหัว
    aim('B-forearm.R', 'B-hand.R', 0.15, 0.8, -0.55)
    aim('B-upperArm.L', 'B-forearm.L', 0.25, 0.9, -0.3)
    aim('B-forearm.L', 'B-hand.L', -0.15, 0.8, -0.55)
    aim('B-thigh.R', 'B-shin.R', -0.1, -0.98, 0.1)
    aim('B-shin.R', 'B-foot.R', -0.02, -0.98, -0.05)
    aim('B-thigh.L', 'B-shin.L', 0.1, -0.98, 0.1)
    aim('B-shin.L', 'B-foot.L', 0.02, -0.98, -0.05)
    to_ground(0.04)

def slam_hit():
    rot('B-spine', 'x', 28)
    rot('B-chest', 'x', 22)
    rot('B-neck', 'x', -25)
    aim('B-upperArm.R', 'B-forearm.R', -0.2, -0.55, 0.8)    # กำปั้นสองข้างทุบลงพื้นหน้าตัว
    aim('B-forearm.R', 'B-hand.R', 0.1, -0.95, 0.3)
    aim('B-upperArm.L', 'B-forearm.L', 0.2, -0.55, 0.8)
    aim('B-forearm.L', 'B-hand.L', -0.1, -0.95, 0.3)
    aim('B-thigh.R', 'B-shin.R', -0.2, -0.6, 0.75)          # ย่อตัวลงเต็มที่
    aim('B-shin.R', 'B-foot.R', -0.05, -0.85, -0.5)
    aim('B-thigh.L', 'B-shin.L', 0.2, -0.6, 0.75)
    aim('B-shin.L', 'B-foot.L', 0.05, -0.85, -0.5)
    to_ground()

def slam_hold():
    slam_hit()
    rot('B-chest', 'x', -4)

slam = make_clip('Golem_Slam', [(1, st), (16, slam_raise), (20, slam_raise), (24, slam_hit), (32, slam_hold), (45, st)])


# ---------- 4. Throw ปาหิน 48 เฟรม — ปล่อยหินที่เฟรม 30 (~0.62) ----------
def throw_pick():
    rot('B-spine', 'x', 30)
    rot('B-chest', 'x', 20)
    rot('B-neck', 'x', -25)
    aim('B-upperArm.R', 'B-forearm.R', -0.15, -0.75, 0.65)  # ก้มหยิบหินจากพื้น
    aim('B-forearm.R', 'B-hand.R', 0.2, -0.85, 0.5)
    aim('B-upperArm.L', 'B-forearm.L', 0.15, -0.75, 0.65)
    aim('B-forearm.L', 'B-hand.L', -0.2, -0.85, 0.5)
    aim('B-thigh.R', 'B-shin.R', -0.2, -0.65, 0.7)
    aim('B-shin.R', 'B-foot.R', -0.05, -0.85, -0.5)
    aim('B-thigh.L', 'B-shin.L', 0.2, -0.65, 0.7)
    aim('B-shin.L', 'B-foot.L', 0.05, -0.85, -0.5)
    to_ground()

def throw_lift():
    rot('B-spine', 'x', -10)
    rot('B-chest', 'x', -14)
    rot('B-neck', 'x', 5)
    aim('B-upperArm.R', 'B-forearm.R', -0.25, 0.8, -0.5)    # ยกหินเหนือหัว เอนหลัง
    aim('B-forearm.R', 'B-hand.R', 0.25, 0.6, -0.75)
    aim('B-upperArm.L', 'B-forearm.L', 0.25, 0.8, -0.5)
    aim('B-forearm.L', 'B-hand.L', -0.25, 0.6, -0.75)
    aim('B-thigh.R', 'B-shin.R', -0.1, -0.95, -0.25)        # ขาขวาถอยหลังรับน้ำหนัก
    aim('B-shin.R', 'B-foot.R', -0.02, -0.98, -0.2)
    aim('B-thigh.L', 'B-shin.L', 0.1, -0.95, 0.3)
    aim('B-shin.L', 'B-foot.L', 0.02, -0.98, 0.05)
    to_ground()

def throw_release():
    rot('B-spine', 'x', 18)
    rot('B-chest', 'x', 16)
    rot('B-neck', 'x', -20)
    aim('B-upperArm.R', 'B-forearm.R', -0.2, 0.35, 0.9)     # เหวี่ยงสองแขนไปหน้า ปล่อยหิน
    aim('B-forearm.R', 'B-hand.R', 0.1, 0.3, 0.95)
    aim('B-upperArm.L', 'B-forearm.L', 0.2, 0.35, 0.9)
    aim('B-forearm.L', 'B-hand.L', -0.1, 0.3, 0.95)
    aim('B-thigh.R', 'B-shin.R', -0.1, -0.95, -0.15)
    aim('B-shin.R', 'B-foot.R', -0.02, -0.98, -0.15)
    aim('B-thigh.L', 'B-shin.L', 0.1, -0.85, 0.5)            # ก้าวขาซ้ายไปหน้า
    aim('B-shin.L', 'B-foot.L', 0.02, -0.98, 0.05)
    to_ground()

def throw_follow():
    throw_release()
    aim('B-upperArm.R', 'B-forearm.R', -0.25, -0.4, 0.88)
    aim('B-forearm.R', 'B-hand.R', -0.1, -0.6, 0.8)
    aim('B-upperArm.L', 'B-forearm.L', 0.25, -0.4, 0.88)
    aim('B-forearm.L', 'B-hand.L', 0.1, -0.6, 0.8)
    to_ground()

throw = make_clip('Golem_Throw', [(1, st), (10, throw_pick), (14, throw_pick), (24, throw_lift), (27, throw_lift),
                                  (30, throw_release), (36, throw_follow), (48, st)])


# ---------- 5. Hit 15 เฟรม ----------
def hit_recoil():
    rot('B-spine', 'x', -4)
    rot('B-chest', 'x', -12)
    rot('B-neck', 'x', 12)
    arms_hang(-0.25, -0.25)
    aim('B-thigh.R', 'B-shin.R', -0.08, -0.98, 0.18)
    aim('B-shin.R', 'B-foot.R', -0.02, -0.98, -0.1)
    aim('B-thigh.L', 'B-shin.L', 0.08, -0.98, 0.18)
    aim('B-shin.L', 'B-foot.L', 0.02, -0.98, -0.1)
    to_ground()

hit = make_clip('Golem_Hit', [(1, st), (5, hit_recoil), (15, st)])


# ---------- 6. Death 60 เฟรม — คุกเข่าแล้วล้มคว่ำหน้า ค้างท่าสุดท้าย ----------
def die_stagger():
    hit_recoil()
    rot('B-chest', 'x', -8)

def die_kneel():
    rot('B-spine', 'x', 20)
    rot('B-chest', 'x', 15)
    rot('B-neck', 'x', -10)
    arms_hang(0.3, 0.3)
    aim('B-thigh.R', 'B-shin.R', -0.15, -0.3, 0.95)          # คุกเข่าสองข้าง
    aim('B-shin.R', 'B-foot.R', -0.05, -0.1, -0.99)
    aim('B-thigh.L', 'B-shin.L', 0.15, -0.3, 0.95)
    aim('B-shin.L', 'B-foot.L', 0.05, -0.1, -0.99)
    to_ground()

def die_fall():
    rot('B-hips', 'x', 50)
    rot('B-spine', 'x', 15)
    rot('B-chest', 'x', 10)
    aim('B-upperArm.R', 'B-forearm.R', -0.3, -0.6, 0.75)
    aim('B-forearm.R', 'B-hand.R', -0.2, -0.7, 0.7)
    aim('B-upperArm.L', 'B-forearm.L', 0.3, -0.6, 0.75)
    aim('B-forearm.L', 'B-hand.L', 0.2, -0.7, 0.7)
    aim('B-thigh.R', 'B-shin.R', -0.15, -0.5, 0.85)
    aim('B-shin.R', 'B-foot.R', -0.05, -0.2, -0.98)
    aim('B-thigh.L', 'B-shin.L', 0.15, -0.5, 0.85)
    aim('B-shin.L', 'B-foot.L', 0.05, -0.2, -0.98)
    to_ground()

def die_ground():
    rot('B-hips', 'x', 88)                                   # นอนคว่ำ ลำตัวราบไปข้างหน้า
    rot('B-neck', 'y', 30)                                   # หันหน้าไปข้าง
    aim('B-upperArm.R', 'B-forearm.R', -0.85, -0.05, 0.5)    # แขนกางบนพื้น
    aim('B-forearm.R', 'B-hand.R', -0.4, -0.05, 0.9)
    aim('B-upperArm.L', 'B-forearm.L', 0.9, -0.05, 0.3)
    aim('B-forearm.L', 'B-hand.L', 0.7, -0.05, -0.7)
    aim('B-thigh.R', 'B-shin.R', -0.15, -0.05, -0.98)        # ขาราบไปข้างหลัง
    aim('B-shin.R', 'B-foot.R', -0.15, 0.1, -0.98)
    aim('B-thigh.L', 'B-shin.L', 0.2, -0.05, -0.97)
    aim('B-shin.L', 'B-foot.L', 0.25, 0.1, -0.96)
    to_ground()

def die_bounce():
    die_ground()
    move('B-hips', 0, 6, 0)

death = make_clip('Golem_Death', [(1, st), (6, die_stagger), (20, die_kneel), (24, die_kneel), (34, die_fall),
                                  (42, die_ground), (46, die_bounce), (50, die_ground), (60, die_ground)])


# ---------- export / render ----------
CLIPS = [idle, walk, slam, throw, hit, death]


def export_clip(action):
    arm.animation_data.action = action
    scene.name = action.name
    s, e = action.frame_range
    scene.frame_start, scene.frame_end = int(s), int(e)
    bpy.ops.export_scene.fbx(filepath=OUT + action.name + '.fbx', object_types={'ARMATURE', 'EMPTY'},
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False,
                             bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0,
                             apply_scale_options='FBX_SCALE_NONE', axis_forward='-Z', axis_up='Y')

if not RENDER:
    for o in list(bpy.data.objects):
        if o.type == 'MESH' or o.name == 'SkinnedMeshes':
            bpy.data.objects.remove(o, do_unlink=True)
    for c in CLIPS:
        export_clip(c)
    print('EXPORTED')
else:
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 300, 380
    cam_data = bpy.data.cameras.new('cam')
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 4.2
    cam = bpy.data.objects.new('cam', cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    fwd = (arm.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()
    right = (arm.matrix_world.to_3x3() @ Vector((-1, 0, 0))).normalized()
    view = right if SIDE else (fwd + right * 0.9).normalized()
    center = Vector((0, 0, GROUND + 1.4))
    cam.location = center + view * 10
    cam.rotation_euler = (-view).to_track_quat('-Z', 'Y').to_euler()
    shots = {'Golem_Idle': [1, 31], 'Golem_Walk': [1, 11, 21, 31], 'Golem_Slam': [1, 16, 24, 32, 45],
             'Golem_Throw': [10, 24, 30, 36], 'Golem_Hit': [5], 'Golem_Death': [6, 20, 34, 42, 60]}
    tag = 'side' if SIDE else 'front'
    for c in CLIPS:
        arm.animation_data.action = c
        for f in shots[c.name]:
            scene.frame_set(f)
            scene.render.filepath = f'/tmp/claude-0/skel/golem_render/{tag}/{c.name}_{f:02d}.png'
            bpy.ops.render.render(write_still=True)
    print('RENDERED')
