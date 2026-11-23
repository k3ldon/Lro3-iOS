#include <stdint.h>
#include <stdlib.h>
#include <string.h>

typedef struct { unsigned char *d; size_t n, p; int bad; } Chunk;

static unsigned char opmap[128];

static void build_opmap(void)
{
    int i;
    for (i = 0; i < 128; i++) opmap[i] = (unsigned char)i;
    for (i = 1; i <= 20; i++) opmap[i] = (unsigned char)(33 + i);  // ADD..CONCAT     -> 34..53
    for (i = 21; i <= 53; i++) opmap[i] = (unsigned char)(i - 20); // LOADI..SHLI     -> 1..33
    opmap[83] = 12;                                               // a second GETTABLE
}

static int need(Chunk *c, size_t k) { if (c->p + k > c->n) { c->bad = 1; return 0; } return 1; }

static uint64_t varint(Chunk *c)
{
    uint64_t x = 0;
    unsigned char b;
    do {
        if (!need(c, 1)) return 0;
        b = c->d[c->p++];
        x = (x << 7) | (b & 0x7F);
    } while ((b & 0x80) == 0);
    return x;
}

static void str(Chunk *c)
{
    size_t size = (size_t)varint(c), n, k;
    if (c->bad || size == 0) return;
    n = size - 1;
    if (!need(c, n)) return;
    for (k = n - 1; n > 1 && k >= 1; k--) c->d[c->p + k] ^= c->d[c->p + k - 1];
    if (n > 0) c->d[c->p] ^= (unsigned char)n;
    c->p += n;
}

static void skip(Chunk *c, size_t k) { if (need(c, k)) c->p += k; }

static void function(Chunk *c, int depth)
{
    uint64_t i, count;
    uint32_t sizecode, key;
    if (depth > 200) { c->bad = 1; return; }
    str(c);
    varint(c);
    varint(c);
    skip(c, 3);

    sizecode = (uint32_t)varint(c);
    key = (sizecode << 16) | (sizecode & 0xFFFF);
    if (!need(c, (size_t)sizecode * 4)) return;
    for (i = 0; i < sizecode; i++) {
        uint32_t inst = (uint32_t)c->d[c->p] | ((uint32_t)c->d[c->p + 1] << 8)
                      | ((uint32_t)c->d[c->p + 2] << 16) | ((uint32_t)c->d[c->p + 3] << 24);
        inst ^= key;
        inst = (inst & ~0x7Fu) | (uint32_t)opmap[inst & 0x7Fu];
        c->d[c->p]     = (unsigned char)inst;
        c->d[c->p + 1] = (unsigned char)(inst >> 8);
        c->d[c->p + 2] = (unsigned char)(inst >> 16);
        c->d[c->p + 3] = (unsigned char)(inst >> 24);
        c->p += 4;
    }

    count = varint(c);
    for (i = 0; i < count; i++) {
        unsigned char tag;
        if (!need(c, 1)) return;
        tag = c->d[c->p++];
        switch (tag) {
        case 0: case 1: break;
        case 0x11: break;
        case 3: skip(c, 8); break;
        case 0x13: skip(c, 8); break;
        case 4: case 0x14: str(c); break;
        default: c->bad = 1; return;
        }
    }

    count = varint(c);
    for (i = 0; i < count; i++) skip(c, 3);

    count = varint(c);
    for (i = 0; i < count; i++) function(c, depth + 1);

    count = varint(c); skip(c, (size_t)count);
    count = varint(c); skip(c, (size_t)count * 8);
    count = varint(c);
    for (i = 0; i < count; i++) { str(c); varint(c); varint(c); }
    count = varint(c);
    for (i = 0; i < count; i++) str(c);
}

unsigned char *ro3_decode_chunk(const unsigned char *src, size_t size, size_t *out_size)
{
    Chunk c;
    unsigned char *d;
    static int opmap_ready = 0;

    if (!src || size < 34 || src[0] != 0x1E || src[1] != 'L' || src[2] != 'u' || src[3] != 'a')
        return NULL;

    if (!opmap_ready) { build_opmap(); opmap_ready = 1; }

    d = (unsigned char *)malloc(size);
    if (!d) return NULL;
    memcpy(d, src, size);
    d[0] = 0x1B;

    c.d = d; c.n = size; c.p = 34; c.bad = 0;
    function(&c, 0);

    if (c.bad) { free(d); return NULL; }
    *out_size = size;
    return d;
}
