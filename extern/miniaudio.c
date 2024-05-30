#ifdef __APPLE__
	#define MA_NO_RUNTIME_LINKING
#endif
#define MA_DEBUG_OUTPUT
#define MINIAUDIO_IMPLEMENTATION
#define MA_DLL
#include "miniaudio/miniaudio.h"
#ifdef __EMSCRIPTEN__
	#include <emscripten.h>

	void main_loop__em()
	{
	}
#endif