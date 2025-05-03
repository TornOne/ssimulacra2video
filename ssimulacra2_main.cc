#include <jxl/memory_manager.h>

#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <fcntl.h>
#include <io.h>
#include <iostream>

#include "lib/jxl/image.h"
#include "lib/jxl/image_bundle.h"
#include "lib/jxl/image_metadata.h"
#include "lib/jxl/base/status.h"
#include "tools/no_memory_manager.h"
#include "tools/ssimulacra2.h"

#define QUIT(M)               \
  fprintf(stderr, "%s\n", M);

static JxlMemoryManager* memory_manager = jpegxl::tools::NoMemoryManager();

extern "C" __declspec(dllexport) void** initialize(uint32_t width, uint32_t height) {
  jxl::ImageMetadata* metadata = new jxl::ImageMetadata();
  metadata->color_encoding = jxl::ColorEncoding::SRGB();
  metadata->SetFloat32Samples();

  jxl::ImageBundle* orig = new jxl::ImageBundle(memory_manager, metadata);
  jxl::ImageBundle* dist = new jxl::ImageBundle(memory_manager, metadata);

  JXL_ASSIGN_OR_QUIT(jxl::Image3F orig_image,
                     jxl::Image3F::Create(memory_manager, width, height),
                     "Failed to create Image3F.");
  JXL_ASSIGN_OR_QUIT(jxl::Image3F dist_image,
                     jxl::Image3F::Create(memory_manager, width, height),
                     "Failed to create Image3F.");

  orig->SetFromImage(std::move(orig_image), jxl::ColorEncoding::SRGB());
  dist->SetFromImage(std::move(dist_image), jxl::ColorEncoding::SRGB());

  void** ret = new void*[8];
  for (size_t i = 0; i < 3; i++) {
    ret[i * 2] = orig_image.PlaneRow(i, 0);
    ret[i * 2 + 1] = dist_image.PlaneRow(i, 0);
  }
  ret[6] = orig;
  ret[7] = dist;

  return ret;
}

extern "C" __declspec(dllexport) double compute_ssimu2(jxl::ImageBundle* orig, jxl::ImageBundle* dist) {
  JXL_ASSIGN_OR_QUIT(Msssim msssim,
                     ComputeSSIMULACRA2(*orig, *dist),
                     "ComputeSSIMULACRA2 failed.");

  orig->OverrideProfile(jxl::ColorEncoding::SRGB());
  dist->OverrideProfile(jxl::ColorEncoding::SRGB());

  return msssim.Score();
}
