LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

# give module name
LOCAL_MODULE := miniaudio
LOCAL_MODULE_FILENAME := miniaudio

# list your C files to compile
LOCAL_SRC_FILES := ../../extern/miniaudio.c

include $(BUILD_SHARED_LIBRARY)