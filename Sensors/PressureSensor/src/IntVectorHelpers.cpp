#include <iostream>
#include <cmath>
#include "IntVectorHelpers.h"

std::vector<int> removeNoiseValues(const std::vector<int>& inputVector, double thresholdFactor) 
{
    // Calculate the mean of the input vector
    double sum = 0;
    for (int value : inputVector) 
    {
        sum += value;
    }
    double mean = sum / inputVector.size();

    // Calculate the standard deviation of the input vector
    double squaredSum = 0;
    for (int value : inputVector) 
    {
        double diff = value - mean;
        squaredSum += diff * diff;
    }
    double variance = squaredSum / inputVector.size();
    double standardDeviation = std::sqrt(variance);

    // Remove noise values based on the threshold factor
    std::vector<int> cleanedVector;
    for (int value : inputVector) 
    {
        if (std::abs(value - mean) <= thresholdFactor * standardDeviation) 
        {
            cleanedVector.push_back(value);
        }
    }
    return cleanedVector;
}

int calculateAverage(const std::vector<int>& numbers) 
{
    int sum = 0;
    for (int num : numbers) 
    {
        sum += num;
    }
    int average = sum / numbers.size();
    return average;
}