#include <errno.h>
#include <fcntl.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <time.h>
#include <unistd.h>

#define EXPORT __attribute__((visibility("default")))

typedef struct { FILE* f; int fd; } fsni_file;

static char g_internal[1024], g_external[1024];
static __thread char t_error[256];

static void fail(const char* what, const char* path)
{
    snprintf(t_error, sizeof t_error, "%s %s: %s", what, path ? path : "", strerror(errno));
    fprintf(stderr, "[Ro3Ios:fsni] %s\n", t_error);
}

static void copy_root(char* dst, const char* src)
{
    snprintf(dst, sizeof g_internal, "%s", src ? src : "");
    size_t n = strlen(dst);
    if (n && dst[n - 1] != '/' && n + 1 < sizeof g_internal) { dst[n] = '/'; dst[n + 1] = 0; }
}

static const char* resolve(const char* path, char* buf, size_t size)
{
    if (!path) return "";
    if (path[0] == '/') return path;
    if (path[0] == '$' && g_external[0])
    {
        snprintf(buf, size, "%s%s", g_external, path + 1);
        return buf;
    }
    if (g_internal[0])
    {
        snprintf(buf, size, "%s%s", g_internal, path);
        return buf;
    }
    return path;
}

EXPORT void fsni_startup(const char* internal_root, const char* external_root)
{
    copy_root(g_internal, internal_root);
    copy_root(g_external, external_root);
    fprintf(stderr, "[Ro3Ios:fsni] startup: internal=%s external=%s\n", g_internal, g_external);
}

EXPORT fsni_file* fsni_open(const char* path, int mode)
{
    char buf[1024];
    const char* real_path = resolve(path, buf, sizeof buf);

    int rw = mode & 1;
    const char* fmode = rw ? (access(real_path, F_OK) == 0 ? "r+b" : "w+b") : "rb";

    FILE* f = fopen(real_path, fmode);
    if (!f)
    {
        fail("open", real_path);
        return NULL;
    }

    fsni_file* handle = (fsni_file*)malloc(sizeof(fsni_file));
    handle->f = f;
    handle->fd = fileno(f);
    return handle;
}

EXPORT int fsni_close(fsni_file* handle)
{
    if (!handle) return 0;
    int res = fclose(handle->f);
    free(handle);
    return res == 0 ? 0 : -1;
}

EXPORT long long fsni_seek(fsni_file* handle, long long offset, int origin)
{
    if (!handle) return -1;
    int whence = origin == 0 ? SEEK_SET : (origin == 1 ? SEEK_CUR : SEEK_END);
    if (fseeko(handle->f, (off_t)offset, whence) != 0)
    {
        fail("seek", NULL);
        return -1;
    }
    return (long long)ftello(handle->f);
}

EXPORT long long fsni_read(fsni_file* handle, void* buffer, long long count)
{
    if (!handle || !buffer) return -1;
    size_t n = fread(buffer, 1, (size_t)count, handle->f);
    if (n == 0 && ferror(handle->f))
    {
        fail("read", NULL);
        return -1;
    }
    return (long long)n;
}

EXPORT long long fsni_write(fsni_file* handle, const void* buffer, long long count)
{
    if (!handle || !buffer) return -1;
    size_t n = fwrite(buffer, 1, (size_t)count, handle->f);
    if (n < (size_t)count)
    {
        fail("write", NULL);
        return -1;
    }
    return (long long)n;
}

EXPORT void* fsni_mmap(fsni_file* handle, long long offset, long long length, int prot, int flags)
{
    if (!handle) return NULL;
    int mprot = PROT_READ;
    if (prot & 2) mprot |= PROT_WRITE;
    int mflags = (flags & 1) ? MAP_SHARED : MAP_PRIVATE;

    void* ptr = mmap(NULL, (size_t)length, mprot, mflags, handle->fd, (off_t)offset);
    if (ptr == MAP_FAILED)
    {
        fail("mmap", NULL);
        return NULL;
    }
    return ptr;
}

EXPORT int fsni_munmap(void* ptr, long long length)
{
    if (!ptr) return 0;
    return munmap(ptr, (size_t)length);
}

EXPORT int fsni_get_last_error(char* buffer, int max_len)
{
    if (!buffer || max_len <= 0) return 0;
    strncpy(buffer, t_error, (size_t)max_len - 1);
    buffer[max_len - 1] = 0;
    return 0;
}
