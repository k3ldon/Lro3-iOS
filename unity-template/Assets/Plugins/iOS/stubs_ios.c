#include <stdint.h>
#include <stddef.h>

#define EXPORT __attribute__((visibility("default")))

// Generic dummy for any function
EXPORT void* _ro3_stub_zero(void) { return NULL; }

// Missing fsni helpers
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <unistd.h>

EXPORT void* fsni_alloc(size_t sz) { return malloc(sz); }
EXPORT void fsni_free(void* ptr) { free(ptr); }
EXPORT void fsni_cleanup(void) { }
EXPORT void* fsni_memdup(const void* src, size_t sz) {
    if (!src || !sz) return NULL;
    void* d = malloc(sz);
    if (d) memcpy(d, src, sz);
    return d;
}
EXPORT char* fsni_strdup(const char* s) { return s ? strdup(s) : NULL; }
EXPORT char* fsni_strndup(const char* s, size_t n) { return s ? strndup(s, n) : NULL; }
EXPORT size_t fsni_strlen(const char* s) { return s ? strlen(s) : 0; }
EXPORT int32_t fsni_exists(const char* path) { return (path && access(path, F_OK) == 0) ? 1 : 0; }
EXPORT int64_t fsni_getsize(const char* path) {
    if (!path) return -1;
    struct stat st;
    return stat(path, &st) == 0 ? (int64_t)st.st_size : -1;
}
EXPORT int32_t fsni_remove(const char* path) { return path ? unlink(path) : -1; }
EXPORT int32_t fsni_rename(const char* oldp, const char* newp) { return (oldp && newp) ? rename(oldp, newp) : -1; }

// Missing sqlite3 stubs
EXPORT int32_t sqlite3_enable_load_extension(void* db, int32_t onoff) { return 0; }
EXPORT int32_t sqlite3_win32_set_directory(int32_t type, void* zValue) { return 0; }

// Wwise stubs
EXPORT int32_t AkSoundEngine_Init(void* settings) { return 1; }
EXPORT int32_t AkSoundEngine_Term(void) { return 1; }
EXPORT uint32_t AkSoundEngine_PostEvent(const char* name, uint64_t gameObj) { return 0; }
EXPORT int32_t AkSoundEngine_RegisterGameObj(uint64_t gameObj) { return 1; }
EXPORT int32_t AkSoundEngine_UnregisterGameObj(uint64_t gameObj) { return 1; }

// Tencent / SDK stubs
EXPORT int32_t ImsdkInit(void* config) { return 0; }
EXPORT int32_t ImsdkLogin(const char* id, const char* sig) { return 0; }
EXPORT int32_t GmeInit(void* config) { return 0; }

// AVProVideo stubs
EXPORT void* AVProVideoPlayer_Create(void) { return NULL; }
EXPORT void AVProVideoPlayer_Dispose(void* player) { }

// xlua / Lua stubs
EXPORT void Lpb_clearCache(void) { }
EXPORT void* gen_cls_indexer(void) { return NULL; }
EXPORT void* gen_cls_newindexer(void) { return NULL; }
EXPORT void* gen_obj_indexer(void) { return NULL; }
EXPORT void* gen_obj_newindexer(void) { return NULL; }
EXPORT int32_t get_error_func_ref(void) { return 0; }
EXPORT void load_error_func(void) { }
EXPORT int32_t pcall_prepare(void) { return 0; }

EXPORT void xlua_csharp_error(void) { }
EXPORT void xlua_csharp_str_error(void) { }
EXPORT void* xlua_get_lib_version(void) { return ""; }
EXPORT int32_t xlua_get_registry_index(void) { return 0; }
EXPORT void xlua_getglobal(void) { }
EXPORT void xlua_getloaders(void) { }
EXPORT int32_t xlua_gettypeid(void) { return 0; }
EXPORT void xlua_gl(void) { }
EXPORT int32_t xlua_is_eq_str(void) { return 0; }
EXPORT size_t xlua_objlen(void) { return 0; }
EXPORT void xlua_pack_decimal(void) { }
EXPORT void xlua_pack_double(void) { }
EXPORT void xlua_pack_float(void) { }
EXPORT void xlua_pack_float2(void) { }
EXPORT void xlua_pack_float3(void) { }
EXPORT void xlua_pack_float4(void) { }
EXPORT void xlua_pack_float5(void) { }
EXPORT void xlua_pack_float6(void) { }
EXPORT void xlua_pack_int16_t(void) { }
EXPORT void xlua_pack_int32_t(void) { }
EXPORT void xlua_pack_int64_t(void) { }
EXPORT void xlua_pack_int8_t(void) { }
EXPORT int32_t xlua_pgettable(void) { return 0; }
EXPORT int32_t xlua_pgettable_bypath(void) { return 0; }
EXPORT int32_t xlua_psettable(void) { return 0; }
EXPORT int32_t xlua_psettable_bypath(void) { return 0; }
EXPORT void xlua_push_csharp_function(void) { }
EXPORT void xlua_push_csharp_wrapper(void) { }
EXPORT void xlua_pushcsobj(void) { }
EXPORT void xlua_pushcstable(void) { }
EXPORT void xlua_pushinteger(void) { }
EXPORT void xlua_pushlstring(void) { }
EXPORT void xlua_pushstruct(void) { }
EXPORT void xlua_pushuint(void) { }
EXPORT int32_t xlua_rawgeti(void) { return 0; }
EXPORT void xlua_rawseti(void) { }
EXPORT void xlua_set_csharp_wrapper_caller(void) { }
EXPORT void xlua_setglobal(void) { }
EXPORT void* xlua_tag(void) { return NULL; }
EXPORT void* xlua_tocsobj_fast(void) { return NULL; }
EXPORT void* xlua_tocsobj_safe(void) { return NULL; }
EXPORT int64_t xlua_tointeger(void) { return 0; }
EXPORT uint64_t xlua_touint(void) { return 0; }
EXPORT void* xlua_tryget_cachedud(void) { return NULL; }
EXPORT void xlua_unpack_decimal(void) { }
EXPORT double xlua_unpack_double(void) { return 0.0; }
EXPORT float xlua_unpack_float(void) { return 0.0f; }
EXPORT void xlua_unpack_float2(void) { }
EXPORT void xlua_unpack_float3(void) { }
EXPORT void xlua_unpack_float4(void) { }
EXPORT void xlua_unpack_float5(void) { }
EXPORT void xlua_unpack_float6(void) { }
EXPORT int16_t xlua_unpack_int16_t(void) { return 0; }
EXPORT int32_t xlua_unpack_int32_t(void) { return 0; }
EXPORT int64_t xlua_unpack_int64_t(void) { return 0; }
EXPORT int8_t xlua_unpack_int8_t(void) { return 0; }
EXPORT int32_t xlua_upvalueindex(int32_t i) { return 0; }
EXPORT int32_t xluaL_loadbuffer(void) { return 0; }

// Lua C API stubs
EXPORT int32_t lua_absindex(void* L, int32_t idx) { return idx; }
EXPORT void* lua_atpanic(void* L, void* panicf) { return NULL; }
EXPORT void lua_callk(void) { }
EXPORT int32_t lua_checkstack(void* L, int32_t n) { return 1; }
EXPORT void lua_close(void* L) { }
EXPORT void lua_createtable(void* L, int32_t narr, int32_t nrec) { }
EXPORT int32_t lua_gc(void* L, int32_t what, int32_t data) { return 0; }
EXPORT int32_t lua_getfield(void* L, int32_t idx, const char* k) { return 0; }
EXPORT int32_t lua_getmetatable(void* L, int32_t idx) { return 0; }
EXPORT int32_t lua_gettop(void* L) { return 0; }
EXPORT const char* lua_getupvalue(void* L, int32_t funcindex, int32_t n) { return NULL; }
EXPORT void lua_insert(void* L, int32_t idx) { }
EXPORT int32_t lua_isint64(void) { return 0; }
EXPORT int32_t lua_isinteger(void* L, int32_t idx) { return 0; }
EXPORT int32_t lua_isnumber(void* L, int32_t idx) { return 0; }
EXPORT int32_t lua_isstring(void* L, int32_t idx) { return 0; }
EXPORT int32_t lua_isuint64(void) { return 0; }
EXPORT void lua_logallocstring(void) { }
EXPORT void lua_logalloctab(void) { }
EXPORT void lua_logfreestring(void) { }
EXPORT void lua_logrehashtab(void) { }
EXPORT int32_t lua_next(void* L, int32_t idx) { return 0; }
EXPORT int32_t lua_pcall(void) { return 0; }
EXPORT void lua_pushboolean(void* L, int32_t b) { }
EXPORT void lua_pushcclosure(void* L, void* fn, int32_t n) { }
EXPORT void lua_pushint64(void) { }
EXPORT void lua_pushlightuserdata(void* L, void* p) { }
EXPORT void lua_pushnil(void* L) { }
EXPORT void lua_pushnumber(void* L, double n) { }
EXPORT void lua_pushthread(void* L) { }
EXPORT void lua_pushuint64(void) { }
EXPORT void lua_pushvalue(void* L, int32_t idx) { }
EXPORT int32_t lua_rawequal(void* L, int32_t idx1, int32_t idx2) { return 0; }
EXPORT int32_t lua_rawget(void* L, int32_t idx) { return 0; }
EXPORT void lua_rawset(void* L, int32_t idx) { }
EXPORT void lua_releaseAllocCb(void) { }
EXPORT void lua_remove(void* L, int32_t idx) { }
EXPORT void lua_replace(void* L, int32_t idx) { }
EXPORT int32_t lua_setfenv(void) { return 0; }
EXPORT int32_t lua_setmetatable(void* L, int32_t idx) { return 0; }
EXPORT void lua_settable(void* L, int32_t idx) { }
EXPORT void lua_settop(void* L, int32_t idx) { }
EXPORT const char* lua_setupvalue(void* L, int32_t funcindex, int32_t n) { return NULL; }
EXPORT int32_t lua_toboolean(void* L, int32_t idx) { return 0; }
EXPORT int64_t lua_toint64(void) { return 0; }
EXPORT const char* lua_tolstring(void* L, int32_t idx, size_t* len) { return NULL; }
EXPORT double lua_tonumber(void* L, int32_t idx) { return 0.0; }
EXPORT void* lua_topointer(void* L, int32_t idx) { return NULL; }
EXPORT void* lua_tothread(void* L, int32_t idx) { return NULL; }
EXPORT uint64_t lua_touint64(void) { return 0; }
EXPORT void* lua_touserdata(void* L, int32_t idx) { return NULL; }
EXPORT int32_t lua_type(void* L, int32_t idx) { return 0; }
EXPORT int32_t luaL_loadfilex(void) { return 0; }
EXPORT int32_t luaL_newmetatable(void* L, const char* tname) { return 0; }
EXPORT void* luaL_newstate(void) { return NULL; }
EXPORT void luaL_openlibs(void* L) { }
EXPORT int32_t luaL_ref(void* L, int32_t t) { return 0; }
EXPORT void luaL_unref(void* L, int32_t t, int32_t ref) { }
EXPORT void luaL_where(void* L, int32_t lvl) { }

// Lua library opens
EXPORT int32_t luaopen_cmsgpack(void* L) { return 0; }
EXPORT int32_t luaopen_i64lib(void* L) { return 0; }
EXPORT int32_t luaopen_lpeg(void* L) { return 0; }
EXPORT int32_t luaopen_pb(void* L) { return 0; }
EXPORT int32_t luaopen_rapidjson(void* L) { return 0; }
EXPORT int32_t luaopen_socket_core(void* L) { return 0; }
EXPORT int32_t luaopen_xlua(void* L) { return 0; }
