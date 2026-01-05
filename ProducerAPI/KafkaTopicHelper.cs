using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace ProducerAPI;

public class KafkaTopicHelper
{
    public static async Task EnsureTopicExists(string bootstrapServers, string topicName)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

        try
        {
            // Check if topic exists
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            if (metadata.Topics.Exists(t => t.Topic == topicName))
            {
                return; // Topic already exists
            }

            // Create topic if it doesn't exist
            await adminClient.CreateTopicsAsync(new TopicSpecification[]
            {
                new TopicSpecification { Name = topicName, ReplicationFactor = 1, NumPartitions = 1 }
            });
        }
        catch (CreateTopicsException e)
        {
            // Ignore if it was created concurrently by another process
            if (e.Results[0].Error.Code != ErrorCode.TopicAlreadyExists)
            {
                Console.WriteLine($"An error occured creating topic {topicName}: {e.Results[0].Error.Reason}");
            }
        }
    }
}