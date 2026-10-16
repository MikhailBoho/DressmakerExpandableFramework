"""Read-only PE/CLR metadata and CIL checks. Never loads/executes game code.
Usage: python audit_contract.py Assembly-CSharp.dll DressmakerMods.dll output.json
Dependencies: pip install dnfile dncil
"""
import hashlib
import json
import sys
from pathlib import Path

import dnfile
from dncil.cil.body.reader import read_method_body_from_bytes


class Assembly:
    def __init__(self, path):
        self.path = Path(path)
        self.pe = dnfile.dnPE(str(path))
        self.tables = self.pe.net.mdtables
        self.types = {i: (str(t.TypeNamespace) + "." + str(t.TypeName)).lstrip(".")
                      for i, t in enumerate(self.tables.TypeDef.rows, 1)}
        if self.tables.NestedClass:
            for n in self.tables.NestedClass.rows:
                self.types[n.NestedClass.row_index] = self.types[n.EnclosingClass.row_index] + "+" + str(n.NestedClass.row.TypeName)
        self.by_name = {name: self.tables.TypeDef.rows[i - 1] for i, name in self.types.items()}
        self.owners = {m.row_index: name for name, t in self.by_name.items() for m in t.MethodList}

    def type_name(self, coded):
        kind, index = coded & 3, coded >> 2
        if kind == 0:
            return self.types[index]
        if kind == 1:
            t = self.tables.TypeRef.rows[index - 1]
            return (str(t.TypeNamespace) + "." + str(t.TypeName)).lstrip(".")
        raise ValueError("Unexpected type code: " + str(coded))

    def decode(self, data, field=False):
        position = 0

        def byte():
            nonlocal position
            b = data[position]
            position += 1
            return b

        def integer():
            a = byte()
            if a < 128:
                return a
            if a < 192:
                return ((a & 63) << 8) | byte()
            return ((a & 31) << 24) | (byte() << 16) | (byte() << 8) | byte()

        def typ():
            kind = byte()
            simple = {1: "void", 2: "bool", 8: "int", 12: "float", 14: "string", 28: "object"}
            if kind in simple:
                return simple[kind]
            if kind in (17, 18):
                return self.type_name(integer())
            if kind == 29:
                return typ() + "[]"
            if kind == 21:
                base = typ()
                return base + "<" + ",".join(typ() for _ in range(integer())) + ">"
            if kind in (19, 30):
                return ("!" if kind == 19 else "!!") + str(integer())
            if kind == 16:
                return typ() + "&"
            raise ValueError("Unsupported signature element " + hex(kind))

        header = byte()
        if field:
            assert header == 6
            return typ()
        if header & 16:
            integer()
        count = integer()
        result = typ()
        return result, [typ() for _ in range(count)]

    def method(self, owner, name, result, args, static=False):
        matches = []
        for item in self.by_name[owner].MethodList:
            m = item.row
            if str(m.Name) == name and self.decode(m.Signature.value) == (result, args):
                matches.append(m)
        assert len(matches) == 1, (owner, name, result, args, "exact signature missing")
        assert bool(matches[0].Flags.mdStatic) == static, (owner, name, "static/instance mismatch")
        return matches[0]

    def field(self, owner, name, expected):
        fields = [f.row for f in self.by_name[owner].FieldList if str(f.row.Name) == name]
        assert len(fields) == 1, (owner, name, "field missing")
        actual = self.decode(fields[0].Signature.value, field=True)
        assert actual == expected, (owner, name, actual, expected)

    def calls(self, method):
        result = []
        for op in read_method_body_from_bytes(self.pe.get_data(method.Rva)).instructions:
            if str(op.opcode) not in ("call", "callvirt"):
                continue
            token = op.operand.value
            if token >> 24 == 6:
                i = token & 0xFFFFFF
                result.append(self.owners[i] + "." + str(self.tables.MethodDef.rows[i - 1].Name))
        return result


game, loader = Assembly(sys.argv[1]), Assembly(sys.argv[2])
checks = []


def method(assembly, owner, name, result, args=(), static=False):
    m = assembly.method(owner, name, result, list(args), static)
    checks.append(owner + "." + name + "(" + ", ".join(args) + ") -> " + result)
    return m


def field(owner, name, kind):
    game.field(owner, name, kind)
    checks.append(owner + "." + name + ": " + kind)


method(loader, "DressmakerMods.StoryAssetRegistry", "Register", "void", ("Framework.ScriptableEnum", "string"), True)
method(loader, "DressmakerMods.StoryAssetRegistry", "Resolve", "!!0", ("string",), True)
method(loader, "DressmakerMods.StoryRequirements", "Create", "QuestRequirement", ("DressmakerMods.ModQuestRequirement", "string"), True)
for name, kind in [("Type", "string"), ("Value", "float"), ("Comparison", "string"), ("Color", "string")]:
    method(loader, "DressmakerMods.ModQuestRequirement", "set_" + name, "void", (kind,))
for name in ["NextPresentation", "PlayDebriefing"]:
    method(game, "FrontDesk", name, "System.Collections.IEnumerator")
for name in ["OnDisable", "CompleteQuest"]:
    method(game, "FrontDesk", name, "void")
for name in ["get_IsPresenting"]:
    method(game, "FrontDesk", name, "bool")
method(game, "GameManager", "SetScene", "void", ("GameManager+Scene", "bool"))
method(game, "GameManager", "get_CurrentScene", "GameManager+Scene")
for name in ["SetSceneMeasuring", "SetSceneSketchbook", "ShowHideOptions"]:
    method(game, "GameManager", name, "void")
method(game, "GameManager", "get_HasPendingUnlocks", "bool")
method(game, "PlayerProgress", "get_Current", "PlayerProgress", static=True)
method(game, "PlayerProgress", "get_HasSaveLoaded", "bool", static=True)
method(game, "PlayerProgress", "get_currentQuestState", "QuestState")
method(game, "PlayerProgress", "SwitchActiveCommission", "void", ("int",))
method(game, "PlayerProgress", "Save", "void", ("PlayerProgress",), True)
method(game, "PlayerProgress", "Load", "PlayerProgress", ("string",), True)
method(game, "MeasurePaper", "ConfirmBodyMeasurement", "void")
method(game, "CharacterDefinition", "get_Alice", "CharacterDefinition", static=True)
method(game, "CharacterDefinition", "set_PrettyName", "void", ("string",))
method(game, "Dress", "DoesDressSatisfyCommission", "bool", ("QuestDefinition",))
for name in ["QualityRequirement", "ColorCompositionRequirement"]:
    method(game, name, "IsMet", "bool", ("Dress", "QuestDefinition"))
method(game, "KnighthoodScene", "ShowKnighthoodEnvelope", "void")
method(game, "KnighthoodScene", "get_IsOpen", "bool")
method(game, "MessengerPigeonUI", "Show", "void")
method(game, "PauseMenu", "get_IsShowing", "bool")
for name, kind in [("frontDesk", "FrontDesk"), ("isInIntro", "bool"), ("commissionSubmitted", "bool"),
                   ("proposedQuest", "QuestDefinition"), ("activeDress", "Dress"),
                   ("commissionForceOutcome", "System.Nullable`1<QuestDefinition+QuestOutcome>"),
                   ("progressDisplayInputBlocker", "UnityEngine.GameObject"), ("optionsMenu", "PauseMenu")]:
    field("GameManager", name, kind)
for name in ["activeCommissions", "completedQuests"]:
    field("PlayerProgress", name, "System.Collections.Generic.List`1<QuestState>")
field("QuestState", "questDefinition", "QuestDefinition")
field("QuestState", "outcome", "QuestDefinition+QuestOutcome")
for name, kind in [("prettyName", "string"), ("budget", "int"), ("questGiverCharacter", "CharacterDefinition"),
                   ("requirements", "System.Collections.Generic.List`1<StoryletRequirement>"),
                   ("completionRequirements", "System.Collections.Generic.List`1<QuestRequirement>")]:
    field("QuestDefinition", name, kind)
field("MeasurePaper", "IsMannequinScene", "bool")
field("CharacterDefinition", "measurements", "Measurements")
field("CharacterDefinition", "silhouette", "UnityEngine.Mesh")
field("CharacterDefinition", "giftQuest", "QuestDefinition")
field("CharacterDefinition", "specialPortraits", "UnityEngine.Sprite[]")
field("CharacterDefinition", "levelUnlocks", "System.Collections.Generic.List`1<CharacterLevelUnlocks>")
for name in ["sprite", "happySprite", "sadSprite", "postcardSprite"]:
    field("CharacterDefinition", name, "UnityEngine.Sprite")
for name in ["rolodexPortraitPosition", "popupPortraitPosition"]:
    field("CharacterDefinition", name, "UnityEngine.Vector2")
for name in ["rolodexPortraitScale", "popupPortraitScale"]:
    field("CharacterDefinition", name, "float")
for name in ["bust", "waist", "hips"]:
    field("Measurements", name, "float")
for owner, name, args, expected in [
    ("FrontDesk", "CompleteQuest", [], ["GameManager.ModifyRelationship", "GameManager.ModifyPrestige", "GameManager.AddGold", "FrontDesk.ArchiveQuest"]),
    ("FrontDesk", "ArchiveQuest", ["QuestState"], ["PlayerProgress.ArchiveQuest", "GameManager.ClearDress", "GameManager.LoadCurrentQuestDress", "PlayerProgress.Save"]),
    ("PlayerProgress", "SwitchActiveCommission", ["int"], ["GameManager.ClearDress", "GameManager.LoadCurrentQuestDress"]),
    ("MeasurePaper", "ConfirmBodyMeasurement", [], ["PlayerProgress.UpdateCharacterMeasurements", "PlayerProgress.Save", "GameManager.SetSceneSketchbook"]),
]:
    calls = game.calls(game.method(owner, name, "void", args))
    assert all(c in calls for c in expected), (owner, name, calls, expected)
    checks.append(owner + "." + name + " native lifecycle calls verified")

output = {"game_sha256": hashlib.sha256(game.path.read_bytes()).hexdigest(),
          "loader_sha256": hashlib.sha256(loader.path.read_bytes()).hexdigest(),
          "checks": checks, "count": len(checks),
          "scope": "Static signatures and native call sites only; neither Unity nor the supplied game code was executed."}
Path(sys.argv[3]).write_text(json.dumps(output, indent=2) + "\n")
print(str(len(checks)) + " native contract checks passed.")
