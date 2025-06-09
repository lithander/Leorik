use bullet_lib::{
    default::{inputs, loader, outputs, Loss, TrainerBuilder},
    lr, optimiser,
    wdl, Activation, LocalSettings, TrainingSchedule, TrainingSteps,
};

const HIDDEN_SIZE: usize = 640;
const SCALE: i32 = 400;
const QA: i16 = 255;
const QB: i16 = 64;

fn main() {
    let mut trainer = TrainerBuilder::default()
        .quantisations(&[QA, QB])
        .optimiser(optimiser::AdamW)
        .loss_fn(Loss::SigmoidMSE)
        .input(inputs::Chess768)
		.output_buckets(outputs::Single)
        .feature_transformer(HIDDEN_SIZE)
        .activate(Activation::SCReLU)
        .add_layer(1)
        .build();

    let schedule = TrainingSchedule {
		// name of checkpoint               
        net_id: "leorik".to_string(),
        eval_scale: SCALE as f32,
        steps: TrainingSteps {
            batch_size: 16_384,
            batches_per_superbatch: 6104,
            start_superbatch: 1,
			// training stops after reaching this
            end_superbatch: 800,
        },
		// This controls the mix of WDL/Eval used, 1.0 = only WDL
        wdl_scheduler: wdl::ConstantWDL { value: 1.0 },
		// Learning rate schedule, this means it starts at 0.001,
		// and every 30 epochs you multiply it by 0.75
        lr_scheduler: lr::StepLR { start: 0.001, gamma: 0.75, step: 30 },
        save_rate: 30,
    };

    trainer.set_optimiser_params(optimiser::AdamWParams::default());

    let settings = LocalSettings { threads: 4, test_set: None, output_directory: "checkpoints", batch_queue_size: 64 };

    // loading directly from a `BulletFormat` file
    let data_loader = loader::DirectSequentialDataLoader::new(&[
	"F:/TD3/FiSh_5K_Q5_v13-FRCv1_01.bullet.bin",
	"F:/TD3/FiSh_5K_Q5_v13-FRCv1_02.bullet.bin",
	"F:/TD3/FiSh_5K_Q5_v13-FRCv1_03.bullet.bin",
	"F:/TD3/FiSh_5K_Q5_v13-FRCv1_04.bullet.bin",
	]);

	//trainer.load_from_checkpoint("leorik-360x");
    trainer.run(&schedule, &settings, &data_loader);
}