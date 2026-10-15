import os, struct, zlib, uuid, shutil

OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "CoachMaster", "Resources", "Kits"))
SLUGS = [
    "placeholder", "norte-maritimo", "sporting-colina", "uniao-valverde", "atletico-ribeira",
    "real-monte-alto", "academica-prado", "estrela-alcantara", "ferroviario", "serra-nova",
    "portuarios", "laranjais", "vila-do-rio", "sc-penhas", "independente-bruma",
]
W, H = 256, 320


def chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def write_png(path, pixels):
    raw = b"".join(b"\x00" + b"".join(bytes(px) for px in pixels[y * W:(y + 1) * W]) for y in range(H - 1, -1, -1))
    ihdr = struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def inside_shirt(u, v):
    if v < 0.44 or v > 0.97:
        return False
    sleeve = 0.72 < v < 0.90 and (u < 0.22 or u > 0.78) and 0.04 < u < 0.96
    body = 0.24 < u < 0.76 and v < 0.94
    neck = v > 0.90 and 0.42 < u < 0.58
    return (body or sleeve) and not neck


def draw():
    pixels = [(0, 0, 0, 0)] * (W * H)
    fill, line, shorts = (58, 62, 70), (170, 176, 186), (42, 45, 52)
    for y in range(H):
        v = y / (H - 1)
        for x in range(W):
            u = x / (W - 1)
            if inside_shirt(u, v):
                edge = u < 0.27 or u > 0.73 or v < 0.47 or v > 0.95
                pixels[y * W + x] = (*(line if edge else fill), 255)
            if 0.28 < v < 0.46 and 0.32 < u < 0.68:
                pixels[y * W + x] = (*shorts, 255)
            if 0.08 < v < 0.26 and ((0.32 < u < 0.42) or (0.58 < u < 0.68)):
                pixels[y * W + x] = (*fill, 255)
    # dashed "slot" box
    for x in range(18, W - 18):
        if (x // 8) % 2 == 0:
            for y in (18, H - 19):
                pixels[y * W + x] = (0, 166, 81, 255)
    for y in range(18, H - 18):
        if (y // 8) % 2 == 0:
            for x in (18, W - 19):
                pixels[y * W + x] = (0, 166, 81, 255)
    return pixels


def meta(path):
    guid = uuid.uuid4().hex
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 1
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spritePixelsToUnits: 100
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  platformSettings: []
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {guid[:16]}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")


def main():
    pixels = draw()
    for slug in SLUGS:
        path = os.path.join(OUT, slug + ".png")
        write_png(path, pixels)
        meta(path)
        print("placeholder", slug)


if __name__ == "__main__":
    main()
