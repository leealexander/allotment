#ifndef __INTVECTORHELPERS_H__
#define __INTVECTORHELPERS_H__

#include <vector>

extern std::vector<int> removeNoiseValues(const std::vector<int>& inputVector, double thresholdFactor);
extern int calculateAverage(const std::vector<int>& numbers);

#endif