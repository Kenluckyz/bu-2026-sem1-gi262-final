"""ท่าเพิ่มของบอสออค (PolyNext Low Poly Orc Warrior) — แพ็กมีท่าโจมตีเดินเท้าแค่ 2 ท่า ทำเพิ่ม 2 ท่า
Orc_Jump  : ย่อตัว -> กระโดด (ยกขวานเหนือหัว) -> ลงพื้นพร้อมจามขวาน  (ตัวลอยไปข้างหน้าทำในโค้ด OrcBoss)
Orc_Guard : ยกโล่ขึ้นบังด้านหน้า (วนลูป)
ใช้ท่าของแพ็กเป็นฐาน (Idle_Battle1 / Attack1) แล้วดัดเพิ่ม
แกน armature: Y = ขึ้น, +Z = หน้า, +X = ซ้ายของตัวละคร  หน่วย cm
"""
import bpy, math, sys, os
from mathutils import Matrix, Vector

A = '/mnt/user-data/uploads/bu-2026-sem1-gi262-final/Assets/PolyNext/Low Poly Orc Warrior/'
OUT = sys.argv[-1] if sys.argv[-1].endswith('/') else '/tmp/orc_out/'
RENDER = '--render' in sys.argv
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
bpy.ops.import_scene.fbx(filepath=A + 'Meshes/Orc_Warrior_Combined2.fbx')
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
mesh_objs = [o for o in bpy.data.objects if o.type == 'MESH']


def load_action(fname):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=A + 'Animations/' + fname + '.fbx')
    for o in set(bpy.data.objects) - before:
        bpy.data.objects.remove(o, do_unlink=True)
    act = [a for a in bpy.data.actions if fname in a.name][0]
    act.use_fake_user = True
    return act

BATTLE = load_action('05_Orc_Warrior_Idle_Battle1')
ATK1 = load_action('07_Orc_Warrior_Attack1')
arm.animation_data_create()


def upd():
    bpy.context.view_layer.update()


def sample(action, frame):
    arm.animation_data.action = action
    if action.slots: arm.animation_data.action_slot = action.slots[0]
    scene.frame_set(frame)
    pose = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}
    arm.animation_data.action = None
    return pose


def apply(pose):
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.matrix_basis = pose[pb.name]
    upd()


AX = {'x': Vector((1, 0, 0)), 'y': Vector((0, 1, 0)), 'z': Vector((0, 0, 1))}

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

def pos(bone, tail=False):
    pb = arm.pose.bones[bone]
    return (pb.tail if tail else pb.head).copy()

def lowest_z():
    dg = bpy.context.evaluated_depsgraph_get()
    low = 1e9
    for m in mesh_objs:
        ev = m.evaluated_get(dg); me = ev.to_mesh(); mw = ev.matrix_world
        for v in me.vertices: low = min(low, (mw @ v.co).z)
        ev.to_mesh_clear()
    return low

def to_ground(extra=0.0):
    dz = GROUND - lowest_z() + extra
    move('root.x', 0, dz / arm.matrix_world.to_scale().x, 0)

def key_all(frame):
    for pb in arm.pose.bones:
        pb.keyframe_insert('rotation_quaternion', frame=frame)
        pb.keyframe_insert('location', frame=frame)
        pb.keyframe_insert('scale', frame=frame)

def make_clip(name, poses):
    action = bpy.data.actions.new(name)
    arm.animation_data.action = action
    for frame, pose in poses:
        arm.animation_data.action = None
        pose()
        arm.animation_data.action = action
        if action.slots: arm.animation_data.action_slot = action.slots[0]
        key_all(frame)
    return action

STANCE = sample(BATTLE, 1)
apply(STANCE)
GROUND = lowest_z()
ROOT_STANCE = pos('root.x')
print('GROUND', GROUND, 'root', ROOT_STANCE)


def atk1(frame, keep_root_xz=True):
    apply(sample(ATK1, frame))
    if keep_root_xz:  # ท่าเดิมก้าวไปข้างหน้า -> ตอนกระโดดโค้ดเป็นคนพาตัวไป ไม่ต้องก้าวเอง
        r = pos('root.x')
        move('root.x', ROOT_STANCE.x - r.x, 0, ROOT_STANCE.z - r.z)


# ---------- 1. Orc_Jump 56 เฟรม : ออกตัวเฟรม 12 (0.2) / ลงพื้น+จามเฟรม 30 (~0.527) ----------
def legs(thigh, shin, foot):
    # หมุนขาจากท่าเดิม (องศา): ต้นขาติดลบ = ยกเข่ามาหน้า, ขาล่างบวก = พับไปหลัง
    for side in ('l', 'r'):
        rot('thigh_stretch.' + side, 'x', thigh)
        rot('leg_stretch.' + side, 'x', shin)
        rot('foot.' + side, 'x', foot)

def legs_bend(deg):
    legs(-deg, deg * 2, -deg)      # ย่อตัว (เท้าราบกับพื้นเหมือนเดิม)

def legs_tuck():
    legs(-65, 100, -20)            # ลอยกลางอากาศ: เข่ายกมาหน้า ขาล่างพับไปหลัง

def legs_reach():
    legs(-10, 5, 10)               # กำลังลง/เพิ่งออกตัว: ขาเหยียดลงหาพื้น

def solve_arm(target, pole):
    # IK 2 ท่อน: ต้นแขน (arm_stretch.l) + แขนท่อนล่าง (forearm_stretch.l) ให้ข้อมือไปอยู่ที่ target
    s = pos('arm_stretch.l'); e = pos('forearm_stretch.l'); w = pos('hand.l')
    l1 = (e - s).length; l2 = (w - e).length
    d = target - s
    dist = min(d.length, (l1 + l2) * 0.999)
    dn = d.normalized()
    a = (l1 * l1 - l2 * l2 + dist * dist) / (2 * dist)
    h = math.sqrt(max(l1 * l1 - a * a, 0.0))
    side = (pole - dn * pole.dot(dn)).normalized()
    elbow = s + dn * a + side * h
    hand_rot = arm.pose.bones['hand.l'].matrix.to_3x3().copy()   # เก็บองศามือเดิมไว้ โล่จะได้หันหน้าเหมือนเดิม
    ex, ey, ez = elbow - s
    aim('arm_stretch.l', 'forearm_stretch.l', ex, ey, ez)
    wx, wy, wz = (s + dn * dist) - pos('forearm_stretch.l')
    aim('forearm_stretch.l', 'hand.l', wx, wy, wz)
    pb = arm.pose.bones['hand.l']
    pb.matrix = Matrix.Translation(pb.head) @ hand_rot.to_4x4() @ Matrix.Diagonal(pb.matrix.to_scale()).to_4x4()
    upd()


def j_stance():
    apply(STANCE)


def leg(side, thigh_dir, shin_dir, foot_dir=None):
    sx = 1 if side == 'l' else -1
    aim('thigh_stretch.' + side, 'leg_stretch.' + side, thigh_dir[0] * sx, thigh_dir[1], thigh_dir[2])
    aim('leg_stretch.' + side, 'foot.' + side, shin_dir[0] * sx, shin_dir[1], shin_dir[2])
    if foot_dir is not None:
        aim('foot.' + side, 'toes_01.' + side, foot_dir[0] * sx, foot_dir[1], foot_dir[2])


def shield_to(x, y, z):
    # มือซ้าย (โล่) ไปที่ตำแหน่ง armature (cm) — IK 2 ท่อน เก็บองศามือเดิมไว้
    solve_arm(Vector((x, y, z)), Vector((1, -0.6, 0)))


def axe_low_back():
    # ง้างขวานไปข้างหลังต่ำ ๆ (มือขวา = -X)
    aim('arm_stretch.r', 'forearm_stretch.r', -0.35, -0.75, -0.55)
    aim('forearm_stretch.r', 'hand.r', -0.1, -0.6, -0.8)


def j_crouch1():
    apply(STANCE)
    rot('spine_01.x', 'x', 14)
    legs_bend(28)
    axe_low_back()
    to_ground()
    shield_to(18, 112, 40)


def j_crouch2():
    apply(STANCE)
    rot('spine_01.x', 'x', 24)
    rot('spine_02.x', 'x', 8)
    legs_bend(42)
    axe_low_back()
    to_ground()
    shield_to(16, 95, 45)


def j_takeoff():
    atk1(7)                                   # แขนขวายกขวานขึ้นเหนือหัว
    rot('spine_01.x', 'x', -6)
    leg('l', (0.15, -1.0, -0.05), (0.05, -1.0, -0.15))   # ขาเหยียดถีบพื้น
    leg('r', (0.15, -1.0, 0.1), (0.05, -1.0, -0.2))
    to_ground()
    shield_to(30, 160, 25)


def j_apex():
    atk1(10)                                  # ขวานเหนือหัว ง้างไปข้างหลัง
    rot('spine_01.x', 'x', -10)
    leg('r', (0.15, -0.25, 0.97), (0.05, -0.95, -0.25))  # เข่าขวายกขึ้นมาหน้า
    leg('l', (0.12, -0.95, -0.25), (0.05, -0.8, -0.6))   # ขาซ้ายห้อยพับไปหลัง
    to_ground()
    shield_to(28, 175, 20)                    # โล่ยกขึ้นสูง


def j_preland():
    atk1(16)                                  # ขวานง้างสุด กำลังจะฟาด
    rot('spine_01.x', 'x', -4)
    leg('r', (0.15, -0.7, 0.7), (0.05, -1.0, -0.05))     # ยืดขาลงหาพื้น
    leg('l', (0.12, -0.98, 0.1), (0.05, -0.95, -0.3))
    to_ground()
    shield_to(40, 140, 20)


def kneel_legs():
    # คุกเข่าซ้ายลงพื้น ขาขวาก้าวไปหน้า (ท่าลงจามแบบใน Reference)
    leg('l', (0.15, -0.95, -0.2), (0.0, -0.12, -1.0), (0.0, -0.3, -0.95))
    leg('r', (0.12, -0.3, 0.95), (0.03, -1.0, -0.05))


def j_land():
    # เท้าแตะพื้น ย่อรับแรง ขวานยังง้างค้างเหนือหัว (ยังไม่ฟาด)
    atk1(16)
    rot('spine_01.x', 'x', 6)
    kneel_legs()
    to_ground()
    shield_to(45, 120, 20)


def j_settle():
    # ลงพื้นแล้ว (โดนครั้งที่ 1 = แรงกระแทกตอนลง) ยกขวานขึ้นไปง้างหลังหัวอีกรอบ
    atk1(10)
    rot('spine_01.x', 'x', 2)
    kneel_legs()
    to_ground()
    shield_to(50, 110, 20)


def j_windup():
    atk1(16)                                  # ง้างสุด กำลังจะจาม
    rot('spine_01.x', 'x', -2)
    kneel_legs()
    to_ground()
    shield_to(48, 120, 18)


def j_impact():
    atk1(22)                                  # ขวานจามลงพื้นหน้าตัว (โดนครั้งที่ 2)
    rot('spine_01.x', 'x', 18)
    rot('spine_02.x', 'x', 10)
    kneel_legs()
    to_ground()
    shield_to(62, 70, 5)                      # โล่กางออกข้างตัว


def j_hold():
    atk1(23)
    rot('spine_01.x', 'x', 22)
    rot('spine_02.x', 'x', 10)
    kneel_legs()
    to_ground()
    shield_to(60, 66, 8)


def j_rise():
    atk1(27)
    rot('spine_01.x', 'x', 10)
    legs_bend(30)
    to_ground()


# 104 เฟรม — Combo 2 จังหวะ
#   ย่อตัว 1-24 / ออกตัวเฟรม 27 / ลอย / เท้ากระแทกพื้นเฟรม 45 (โดนครั้งที่ 1)
#   ยกขวานง้างใหม่ 52-62 / ขวานถึงพื้นเฟรม 66 (โดนครั้งที่ 2) / ค้าง / ลุกขึ้น
#   takeoffPoint = 26/103 = 0.252, landPoint = 44/103 = 0.427, hitPoint = 65/103 = 0.631
#   (ห่างกัน ~0.7 วิ มากกว่าเวลาอมตะหลังโดนตีของ Boat 0.6 วิ ไม่งั้นครั้งที่ 2 จะไม่เข้า)
jump = make_clip('Orc_Jump', [(1, j_stance), (12, j_crouch1), (24, j_crouch2), (27, j_takeoff), (34, j_apex),
                              (41, j_preland), (45, j_land), (52, j_settle), (62, j_windup), (67, j_impact),
                              (70, j_impact), (82, j_hold), (94, j_rise), (104, j_stance)])


def guard_pose(breath):
    apply(STANCE)
    rot('spine_01.x', 'x', 14 + breath)      # โน้มตัวมาหน้า หลบหลังโล่
    rot('neck.x', 'x', -10)
    legs_bend(22)
    to_ground()
    w = pos('hand.l')
    target = Vector((w.x * 0.3 + 4, 138 + breath, max(w.z, 0) + 30))   # ข้อมือขึ้นมาระดับหน้า ยื่นโล่ไปข้างหน้า
    solve_arm(target, Vector((1, -0.6, 0)))
    # แขนขวา (ถือขวาน) ง้างไว้ข้างตัว พร้อมสวน
    rot('arm_stretch.r', 'z', -8)

guard = make_clip('Orc_Guard', [(1, lambda: guard_pose(0)), (21, lambda: guard_pose(2)), (41, lambda: guard_pose(0))])

CLIPS = [jump, guard]
for a in CLIPS:
    for fc in a.fcurves if hasattr(a, 'fcurves') else []:
        for k in fc.keyframe_points: k.interpolation = 'BEZIER'

if RENDER:
    sc = scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'SINGLE'
    sc.display.shading.single_color = (0.55, 0.62, 0.45)
    sc.render.resolution_x = 300; sc.render.resolution_y = 340
    w = bpy.data.worlds.new('w'); sc.world = w
    cam = bpy.data.cameras.new('c'); co = bpy.data.objects.new('c', cam); sc.collection.objects.link(co); sc.camera = co
    yaw = float(os.environ.get('YAW', '35'))
    d = 6.0
    co.location = Vector((math.sin(math.radians(yaw)) * d, -math.cos(math.radians(yaw)) * d, 1.4))
    co.rotation_euler = (Vector((0, 0, 0.9)) - co.location).to_track_quat('-Z', 'Y').to_euler()
    # พื้น
    bpy.ops.mesh.primitive_plane_add(size=6, location=(0, 0, GROUND))
    for a, frames in ((jump, [1, 24, 34, 41, 45, 47, 49, 64]),):
        arm.animation_data.action = a
        if a.slots: arm.animation_data.action_slot = a.slots[0]
        for f in frames:
            sc.frame_set(f)
            sc.render.filepath = OUT + '%s_f%02d.png' % (a.name, f)
            bpy.ops.render.render(write_still=True)
else:
    for o in mesh_objs:
        bpy.data.objects.remove(o, do_unlink=True)
    for a in CLIPS:
        arm.animation_data.action = a
        if a.slots: arm.animation_data.action_slot = a.slots[0]
        scene.frame_start = int(a.frame_range[0]); scene.frame_end = int(a.frame_range[1])
        bpy.ops.export_scene.fbx(filepath=OUT + a.name + '.fbx', use_selection=False, object_types={'ARMATURE', 'EMPTY'},
                                 add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False,
                                 bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0,
                                 apply_scale_options='FBX_SCALE_NONE', axis_forward='-Z', axis_up='Y')
print('DONE')
