#include <stdint.h>
#include <stddef.h>

#define EXPORT __attribute__((visibility("default")))

EXPORT int32_t AkSoundEngine_Init(void* settings) { return 1; }
EXPORT int32_t AkSoundEngine_Term(void) { return 1; }
EXPORT uint32_t AkSoundEngine_PostEvent(const char* name, uint64_t gameObj) { return 0; }
EXPORT int32_t AkSoundEngine_RegisterGameObj(uint64_t gameObj) { return 1; }
EXPORT int32_t AkSoundEngine_UnregisterGameObj(uint64_t gameObj) { return 1; }

EXPORT int32_t ImsdkInit(void* config) { return 0; }
EXPORT int32_t ImsdkLogin(const char* id, const char* sig) { return 0; }
EXPORT int32_t GmeInit(void* config) { return 0; }

EXPORT void* AVProVideoPlayer_Create(void) { return NULL; }
EXPORT void AVProVideoPlayer_Dispose(void* player) { }
